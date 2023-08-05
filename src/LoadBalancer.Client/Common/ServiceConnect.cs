using GameCarrier.Async;
using GameCarrier.Common;
using LoadBalancer.Common;
using LoadBalancer.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LoadBalancer.Client.Common
{
    partial class ServiceConnect : IServiceConnect, IDisposable
    {
        public enum ConnectionState
        {
            Unknown = 0,
            Connecting,
            Connected,
            Disconnecting,
            Disconnected,
        }

        public ServiceConnect()
        {
            Start();
        }

        partial void Start();
        partial void Stop();

        #region Properties

        protected readonly object lockObject = new object();

        private int timeoutConnect = 30000;
        public int TimeoutConnect
        {
            get { lock (lockObject) return timeoutConnect; }
            set { lock (lockObject) timeoutConnect = value; }
        }

        private int timeoutDisconnect = 30000;
        public int TimeoutDisconnect
        {
            get { lock (lockObject) return timeoutDisconnect; }
            set { lock (lockObject) timeoutDisconnect = value; }
        }

        private int timeoutSend = 300000;
        public int TimeoutSend
        {
            get { lock (lockObject) return timeoutSend; }
            set { lock (lockObject) timeoutSend = value; }
        }

        private bool throwExceptions;
        public bool ThrowExceptions
        {
            get { lock (lockObject) return throwExceptions; }
            set { lock (lockObject) throwExceptions = value; }
        }

        private ConnectionState _state = ConnectionState.Disconnected;
        private ConnectionState State
        {
            get { lock (lockObject) return _state; }
            set { lock (lockObject) _state = value; }
        }

        private ClientAdapter _client;
        private ClientAdapter Client
        {
            get { lock (lockObject) return _client; }
            set { lock (lockObject) _client = value; }
        }

        public bool IsConnected => State == ConnectionState.Connected;
        private bool IsDisconnected => State == ConnectionState.Disconnected;

        private long operationCounter;
        private long QueryNextOperationCounter()
        {
            lock (lockObject)
                return ++operationCounter;
        }

        #endregion

        public override string ToString() => $"{Client}";

        #region Connect

        public event Action OnConnected;

        private Task<Result> connectTask;

        public async Task<Result> Connect(Endpoint endpoint)
        {
            if (IsConnected) return Result.Ok();

            try
            {
                Task<Result> task;
                lock (lockObject)
                {
                    if (disconnectTask != null)
                        throw new ResultException(CommonErrors.Error_ConnectException, CommonMessages.Message_AwaitingDisconnect);

                    if (connectTask == null)
                        connectTask = InitiateConnect(endpoint);

                    task = connectTask;
                }

                return await task;
            }
            finally
            {
                lock (lockObject)
                    connectTask = null;
            }
        }

        private async Task<Result> InitiateConnect(Endpoint endpoint)
        {
            State = ConnectionState.Connecting;

            // Erase buggy client
            var client = Client;
            if (client != null && !client.Connected)
                client = ClearClient();

            if (client != null)
                throw new ArgumentException("client should be null here");

            lock (lockObject)
            {
                string address = $"{endpoint.Protocol}://{endpoint.Address}:{endpoint.Port}";
                _client = new ClientAdapter(address, endpoint.AppName);
                _client.OnMessageEvent += Client_OnMessage;
                _client.OnDisconnectEvent += Client_OnDisconnect;
                client = _client;
            }

            await client.ConnectAsync(timeout: TimeoutConnect)
                .Named(out var operation)
                .ExecuteAsync();
            if (!operation.rt.WasCompleted)
            {
                client = ClearClient();
                State = ConnectionState.Disconnected;
                return ErrorResult(operation);
            }

            State = ConnectionState.Connected;
            OnConnected?.Invoke();
            return Result.Ok();
        }

        private ClientAdapter ClearClient()
        {
            lock (lockObject)
            {
                if (_client == null) return null;
                _client.OnMessageEvent -= Client_OnMessage;
                _client.OnDisconnectEvent -= Client_OnDisconnect;
                _client = null;
                return null;
            }
        }

        #endregion

        #region Disconnect

        protected virtual void OnDisconnect(string reason) { }

        public event Action<string> OnDisconnected;

        private Task<Result> disconnectTask;
        public async Task<Result> Disconnect()
        {
            if (IsDisconnected) return Result.Ok();

            try
            {
                Task<Result> task;
                lock (lockObject)
                {
                    if (State == ConnectionState.Disconnecting && disconnectTask == null)
                        return Result.Ok();     // enter Disconnect after counterpart-initiated Disconnect

                    if (connectTask != null)
                        throw new ResultException(CommonErrors.Error_ConnectException, CommonMessages.Message_AwaitingConnect);

                    if (disconnectTask == null)
                        disconnectTask = InitiateDisconnect();

                    task = disconnectTask;
                }

                return await task;
            }
            finally
            {
                lock (lockObject)
                    disconnectTask = null;
            }
        }

        private async Task<Result> InitiateDisconnect()
        {
            State = ConnectionState.Disconnecting;

            var client = Client;
            if (client != null && client.Connected)
            {
                client.OnDisconnectEvent -= Client_OnDisconnect;
                await client.DisconnectAsync(timeout: TimeoutDisconnect).ExecuteAsync();
            }

            return FinishDisconnect(new DisconnectEventArgs());
        }

        private void Client_OnDisconnect(object sender, DisconnectEventArgs e)
        {
            State = ConnectionState.Disconnecting;

            _ = FinishDisconnect(e);
        }

        private Result FinishDisconnect(DisconnectEventArgs e)
        {
            if (IsDisconnected) return Result.Ok();

            ClearClient();

            State = ConnectionState.Disconnected;

            // string reason = e.Reason;
            string reason = null;
            OnDisconnect(reason);
            OnDisconnected?.Invoke(reason);

            return Result.Ok();
        }

        #endregion

        #region Private methods

        private ClientAdapter GetConnectedClient()
        {
            if (!IsConnected)
                throw new ResultException(CommonErrors.Error_ConnectException, CommonMessages.Message_Disconnected);

            var client = Client;
            if (client == null) // terminate operations after disconnect
                throw new ResultException(CommonErrors.Error_ConnectException, CommonMessages.Message_Disconnected);

            return client;
        }

        private Result ErrorResult(AsyncMessage operation)
        {
            if (operation.rt.WasCancelled)
            {
                if (ThrowExceptions)
                    throw new OperationCanceledException(operation.OperationError);
                else
                    return Result.Error(CommonErrors.Error_ConnectException, operation.OperationError);
            }

            if (operation.rt.WasTimedOut)
            {
                if (ThrowExceptions)
                    throw new TimeoutException();
                else
                    return Result.Error(CommonErrors.Error_ConnectException, CommonMessages.Message_TimedOut);
            }

            if (operation.rt.Exception != null)
            {
                if (ThrowExceptions)
                    throw operation.rt.Exception;
                else
                    return Result.Error(CommonErrors.Error_ConnectException, operation.rt.Exception.Message);
            }

            return new Result();
        }

        #endregion

        #region Send

        public void SendRealtime(int code, Action<BinaryWriter> write)
        {
            GetConnectedClient();

            byte[] data;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.WriteOperationType(OperationType.Realtime);
                writer.Write(code);
                if (write != null)
                    write(writer);
                data = stream.ToArray();
            }

            GetConnectedClient().Send(data);
        }

        public void RaiseEvent(KeyType name, object parameters) => RaiseEvent(name, parameters.Serialize());

        public void RaiseEvent(KeyType name, KeyValueCollection parameters = null)
        {
            GetConnectedClient();

            if (parameters == null) parameters = new KeyValueCollection();
            byte[] data;
            long counter = QueryNextOperationCounter();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.WriteOperationType(OperationType.Event);
                writer.Write(counter);
                writer.WriteKeyType(name);
                writer.WriteDictionary(parameters);
                data = stream.ToArray();
            }

            GetConnectedClient().Send(data);
        }

        public async Task<T> CallMethod<T>(KeyType name, object parameters = null,
            bool heavyParameters = false, bool heavyResult = false, int? timeout = null) where T : new()
        {
            var map = !heavyParameters ? parameters.Serialize() : await parameters.SerializeAsync();
            var resultMap = await CallMethod(name, map, timeout);
            var result = !heavyResult ? resultMap.Materialize<T>() : await resultMap.MaterializeAsync<T>();
            return result;
        }

        public async Task<KeyValueCollection> CallMethod(KeyType name, KeyValueCollection parameters = null, int? timeout = null)
        {
            GetConnectedClient();

            if (parameters == null) parameters = new KeyValueCollection();
            byte[] data;
            long counter = QueryNextOperationCounter();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.WriteOperationType(OperationType.Method);
                writer.Write(counter);
                writer.WriteKeyType(name);
                writer.WriteDictionary(parameters);
                data = stream.ToArray();
            }

            KeyValueCollection result = null;
            var task = new AsyncMessage()
                .Named(out var operation)
                .SetTimeout(timeout ?? TimeoutSend)
                .AddBody(() =>
                {
                    if (!IsConnected)
                    {
                        operation.Cancel("disconnected");
                        return;
                    }

                    try
                    {
                        GetConnectedClient().Send(data);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Send exception");
                        operation.SetException(ex);
                    }
                })
                .AddSubscription(OnDisconnected
                    .Event(h => OnDisconnected += h, h => OnDisconnected -= h)
                    .Handler(r =>
                    {
                        operation.Cancel("disconnected");
                    }))
                    .AddSubscription(OnMethodCompleted
                    .Event(h => OnMethodCompleted += h, h => OnMethodCompleted -= h)
                    .Handler(call =>
                    {
                        try
                        {
                            if (call.Name == name && call.Counter == counter)
                            {
                                result = call.Parameters;
                                var status = result.GetValue<KeyType>(CommonParameters.Status);
                                var statusName = status.ToDisplayName(HandlerTable.GetErrorType(call));
                                result.SetValue(CommonParameters.StatusName, statusName);
                                operation.Complete();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "OnMethodCompleted unhandled exception");
                            operation.SetException(ex);
                        }
                    }))
                    .ExecuteAsync();

            await task;

            if (!operation.rt.WasCompleted)
                return ErrorResult(operation).Serialize();

            return result;
        }

        #endregion

        #region Receive

        public event Action<ClientCallContext> OnRealtimeOperation;
        public event Action<ClientCallContext> OnParseMetadata;
        public event Action<ClientCallContext> OnEventReceived;
        private event Action<ClientCallContext> OnMethodCompleted;

        private void Client_OnMessage(object sender, MessageEventArgs args)
        {
            var data = args.RawData;

            try
            {
                ClientCallContext call;
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    var type = reader.ReadOperationType();
                    if (type == OperationType.Realtime)
                    {
                        int code = reader.ReadInt32();
                        call = new ClientCallContext(type, code, reader);
                        if (Logger.IsTraceEnabled)
                            Logger.LogTrace($"OnRealtimeOperation {call.Name}");
                        OnRealtimeOperation(call);
                        return;
                    }

                    var counter = reader.ReadInt64();
                    var name = reader.ReadKeyType();
                    var parameters = reader.ReadDictionary();
                    call = new ClientCallContext(type, counter, name, parameters);
                }

                OnParseMetadata?.Invoke(call);
                call.OperationName = call.Name.ToDisplayName(HandlerTable.GetMethodType(call));

                switch (call.Type)
                {
                    case OperationType.Event:
                        if (Logger.IsTraceEnabled)
                            Logger.LogTrace($"OnEventReceived {call.Name} ({call.OperationName})");
                        OnEventReceived?.Invoke(call);
                        break;

                    case OperationType.Method:
                        if (Logger.IsTraceEnabled)
                            Logger.LogTrace($"OnMethodCompleted {call.Name} ({call.OperationName})");
                        OnMethodCompleted?.Invoke(call);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception in OnMessage");
            }
        }

        #endregion

        #region Ping / Echo / SelectClosestService

        public Task<KeyValueCollection> Echo(KeyValueCollection parameters = null, int? timeout = null) =>
            CallMethod(CommonMethods.Echo, parameters, timeout: timeout);

        public async Task<PingResult> Ping(KeyValueCollection parameters = null, int? timeout = null)
        {
            DateTime start = DateTime.UtcNow;
            var result = await CallMethod<PingResult>(CommonMethods.Echo, parameters, timeout: timeout);
            result.PingMiliseconds = (int)DateTime.UtcNow.Subtract(start).TotalMilliseconds;
            return result;
        }

        public static async Task<PingResult> Ping(Endpoint endpoint, KeyValueCollection parameters = null, int? timeout = null)
        {
            using (var connect = new ServiceConnect())
            {
                await connect.Connect(endpoint);
                if (!connect.IsConnected)
                    return new PingResult().Error(CommonErrors.Error_ConnectException, CommonMessages.Message_CantConnect);

                return await connect.Ping(parameters, timeout: timeout);
            }
        }

        public static async Task<SelectClosestServiceResult> SelectClosestService(Endpoint[] endpoints, KeyValueCollection parameters = null, int? timeout = null)
        {
            Endpoint closestEndpoint = null;
            PingResult bestPingResult = null;
            foreach (var endpoint in endpoints)
            {
                var result = await Ping(endpoint, parameters, timeout: timeout);
                if (!result.IsOk) continue;
                if (bestPingResult == null || result.PingMiliseconds < bestPingResult.PingMiliseconds)
                {
                    bestPingResult = result;
                    closestEndpoint = endpoint;
                }
            }

            if (closestEndpoint == null)
                return new SelectClosestServiceResult().Error(CommonErrors.Error_ConnectException, CommonMessages.Message_CantConnect);

            return new SelectClosestServiceResult { ServiceEndpoint = closestEndpoint }.Ok();
        }

        #endregion

        #region Dispose

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    Stop();

                if (!IsDisconnected)
                    _ = Disconnect();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~ServiceConnect()
        {
            Dispose(false);
        }

        #endregion
    }

    partial class ServiceConnect<C> : ServiceConnect, IServiceConnect<C> where C : ServiceClientBase, new()
    {
        private C service;
        private C ServiceSafe { get { lock (lockObject) return service; } }
        public C Service
        {
            get
            {
                lock (lockObject)
                {
                    if (service == null)
                        service = new C() { Connect = this };
                    return service;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                var service = ServiceSafe;
                if (service != null)
                    service.Dispose();
            }
        }
    }
}
