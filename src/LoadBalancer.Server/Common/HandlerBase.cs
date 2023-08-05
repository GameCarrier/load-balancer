using GameCarrier.Adapter;
using LoadBalancer.Common;
using LoadBalancer.Extensions;

namespace LoadBalancer.Server.Common
{
    public interface _IInternalHandler
    {
        void Send(OperationType type, long operationCounter, KeyType name, KeyValueCollection parameters);
    }

    public class HandlerBase : _IInternalHandler, IDisposable
    {
        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<HandlerBase>();

        public ConnectionInfo ConnectionInfo { get; internal set; }

        public ServiceBase Service { get; internal set; }

        internal int Index { get; set; }

        internal GcConnection Connection { get; private set; }

        public bool IsConnected => Connection.Connected;

        public bool IsAuthenticated { get; protected set; }

        public readonly IServerThread Thread = new ServerThread();

        #region Init

        internal void Configure(IntPtr connenctionHandle)
        {
            Connection = new GcConnection(connenctionHandle, false);
            Connection.OnMessageEvent += Connection_OnMessageEvent;
            Connection.OnDisconnectEvent += Connection_OnDisconnectEvent;
            Connection.OnIssueEvent += Connection_OnIssueEvent;
            ((ServerThread)Thread).Start();
        }

        private void Connection_OnIssueEvent(object sender, GameCarrier.Common.IssueEventArgs e)
        {
            // TODO: no callback here
        }

        private void Connection_OnMessageEvent(object sender, GameCarrier.Common.MessageEventArgs e)
        {
            OnMessage(e.RawData);
        }

        private void Connection_OnDisconnectEvent(object sender, GameCarrier.Common.DisconnectEventArgs e)
        {
            Connection.OnMessageEvent -= Connection_OnMessageEvent;
            Connection.OnDisconnectEvent -= Connection_OnDisconnectEvent;
            Connection.OnIssueEvent -= Connection_OnIssueEvent;

            // string reason = e.Reason;
            string reason = null;
            var parameters = new KeyValueCollection
            {
                { CommonParameters.Reason, reason }
            };
            var call = new CallContext(this, OperationType.Event, 0, CommonMethods.Disconnect, parameters);
            Thread.Enqueue(call, () => { OnDisconnected(call); Disconnected?.Invoke(); return true; });

            ((ServerThread)Thread).Stop();
        }

        #endregion

        #region Disconnect

        protected virtual void OnDisconnected(CallContext call) { }

        public event Action Disconnected;

        public void Disconnect() => Connection.Disconnect();

        #endregion

        #region Receive

        protected virtual void OnMessage(byte[] data)
        {
            try
            {
                CallContext call;
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    var type = reader.ReadOperationType();
                    if (type == OperationType.Realtime)
                    {
                        int code = reader.ReadInt32();
                        call = new CallContext(this, type, code, reader);

                        if (Logger.IsTraceEnabled)
                            Logger.LogTrace($"OnRealtimeOperation {call.Code}");

                        OnRealtimeOperation(call);
                        return;
                    }

                    long counter = reader.ReadInt64();
                    var name = reader.ReadKeyType();
                    var parameters = reader.ReadDictionary();
                    call = new CallContext(this, type, counter, name, parameters);
                }

                HandlerTable.ParseMetadata(this, call);
                call.OperationName = call.Name.ToDisplayName(HandlerTable.GetMethodType(call));

                switch (call.Type)
                {
                    case OperationType.Event:
                        if (Logger.IsTraceEnabled)
                            Logger.LogTrace($"OnEventReceived {call.Name} ({call.OperationName})");

                        Thread.Enqueue(call, () => { OnEventReceived(call); return true; });
                        break;

                    case OperationType.Method:
                        if (call.Name == CommonMethods.Echo)
                        {
                            call.Complete(call.Parameters);
                            return;
                        }

                        if (Logger.IsTraceEnabled)
                            Logger.LogTrace($"OnMethodCalled {call.Name} ({call.OperationName})");

                        Thread.Enqueue(call, () => { OnMethodCalled(call); return true; });
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception in OnMessage");
            }
        }

        protected virtual void OnRealtimeOperation(CallContext call) { }

        protected virtual void OnMethodCalled(CallContext call) => HandlerTable.ExecuteHandler(this, call);

        protected virtual void OnEventReceived(CallContext call) => HandlerTable.ExecuteHandler(this, call);

        #endregion

        #region Send

        protected void SendRealtime(int code, Action<BinaryWriter> write)
        {
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

            Connection.SendMessage(data);
        }

        protected void RaiseEvent(KeyType name, object parameters) => RaiseEvent(name, parameters.Serialize());

        protected void RaiseEvent(KeyType name, KeyValueCollection parameters = null) =>
            ((_IInternalHandler)this).Send(OperationType.Event, 0, name, parameters);

        void _IInternalHandler.Send(OperationType type, long operationCounter, KeyType name, KeyValueCollection parameters)
        {
            if (parameters == null) parameters = new KeyValueCollection();
            byte[] data;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.WriteOperationType(type);
                writer.Write(operationCounter);
                writer.WriteKeyType(name);
                writer.WriteDictionary(parameters);
                data = stream.ToArray();
            }

            Connection.SendMessage(data);   // Exception here
        }

        #endregion

        #region Dispose

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (IsConnected)
                    Disconnect();

                if (disposing)
                    ((ServerThread)Thread).Dispose();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~HandlerBase()
        {
            Dispose(false);
        }

        #endregion
    }
}
