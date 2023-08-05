using LoadBalancer.Common;
using LoadBalancer.Extensions;
using System;

namespace LoadBalancer.Client.Common
{
    partial class ServiceClientBase : IDisposable
    {
        private ServiceConnect connect;
        protected internal ServiceConnect Connect
        {
            get => connect;
            set
            {
                if (connect != null)
                {
                    connect.OnConnected -= OnConnected;
                    connect.OnRealtimeOperation -= OnRealtimeOperation;
                    connect.OnParseMetadata -= OnParseMetadata;
                    connect.OnEventReceived -= OnEventReceived;
                    connect.OnDisconnected -= Connect_OnDisconnected;
                }

                connect = value;

                if (connect != null)
                {
                    connect.OnConnected += OnConnected;
                    connect.OnRealtimeOperation += OnRealtimeOperation;
                    connect.OnParseMetadata += OnParseMetadata;
                    connect.OnEventReceived += OnEventReceived;
                    connect.OnDisconnected += Connect_OnDisconnected;
                }
            }
        }

        private void Connect_OnDisconnected(string reason)
        {
            var parameters = new KeyValueCollection
            {
                { CommonParameters.Reason, reason }
            };
            OnDisconnected(new ClientCallContext(OperationType.Event, 0, CommonMethods.Disconnect, parameters));
        }

        public bool IsConnected => Connect != null && Connect.IsConnected;

        protected virtual void OnConnected() { }
        protected virtual void OnParseMetadata(ClientCallContext call) => HandlerTable.ParseMetadata(this, call);
        protected virtual void OnRealtimeOperation(ClientCallContext call) { }
        protected virtual void OnEventReceived(ClientCallContext call) => HandlerTable.ExecuteHandler(this, call);
        protected virtual void OnDisconnected(ClientCallContext call) { }

        #region Dispose

        protected virtual void Dispose(bool disposing) { }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~ServiceClientBase()
        {
            Dispose(false);
        }

        #endregion
    }
}
