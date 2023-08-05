using LoadBalancer.Server.Common;

namespace LoadBalancer.Client.Common
{
    internal partial class ClientCallContext { }

    internal partial class ServiceClientBase { }

    internal partial interface IServiceConnect
    {
        IServerThread Thread { get; }
        IScheduledItem SchedulerConnect { get; set; }
    }

    internal partial interface IServiceConnect<out C> { }

    internal partial class ServiceConnect
    {
        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<ServiceConnect>();

        class ClientAdapter : GameCarrier.Adapter.GcClientConnection
        {
            public ClientAdapter(string address, string appName) : base(address, appName) { }
        }

        public readonly IServerThread Thread = new ServerThread();
        IServerThread IServiceConnect.Thread => Thread;
        public IScheduledItem SchedulerConnect { get; set; }

        partial void Start()
        {
            ((ServerThread)Thread).Connect = this;
            ((ServerThread)Thread).Start();
        }

        partial void Stop()
        {
            ((ServerThread)Thread).Stop();
        }
    }

    internal partial class ServiceConnect<C> : ServiceConnect where C : ServiceClientBase, new() { }
}
