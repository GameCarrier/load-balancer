using LoadBalancer.Common;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LoadBalancer.Client.Common
{
    partial interface IServiceConnect : IDisposable
    {
        bool IsConnected { get; }
        int TimeoutConnect { get; set; }
        int TimeoutDisconnect { get; set; }
        int TimeoutSend { get; set; }
        bool ThrowExceptions { get; set; }

        Task<Result> Connect(Endpoint endpoint);
        Task<Result> Disconnect();

        event Action OnConnected;
        event Action<string> OnDisconnected;

        void SendRealtime(int code, Action<BinaryWriter> write);
        void RaiseEvent(KeyType name, object parameters);
        void RaiseEvent(KeyType name, KeyValueCollection parameters = null);
        Task<T> CallMethod<T>(KeyType name, object parameters,
            bool heavyParameters = false, bool heavyResult = false, int? timeout = null) where T : new();
        Task<KeyValueCollection> CallMethod(KeyType name, KeyValueCollection parameters = null, int? timeout = null);

        event Action<ClientCallContext> OnRealtimeOperation;
        event Action<ClientCallContext> OnEventReceived;

        Task<KeyValueCollection> Echo(KeyValueCollection parameters = null, int? timeout = null);
        Task<PingResult> Ping(KeyValueCollection parameters = null, int? timeout = null);
    }

    partial interface IServiceConnect<out C> : IServiceConnect
    {
        C Service { get; }
    }
}
