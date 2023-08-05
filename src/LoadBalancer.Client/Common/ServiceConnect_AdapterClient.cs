namespace LoadBalancer.Client.Common
{
    public partial class ClientCallContext { }

    public partial class ServiceClientBase { }

    public partial interface IServiceConnect { }

    public partial interface IServiceConnect<out C> { }

    public partial class ServiceConnect
    {
        #region Static methods

        public static GameCarrier.Clients.GcClientMode CurrentClientLibraryMode =>
            GameCarrier.Clients.ClientBase.Manager.ClientMode;

        public static void InitClientLibraryMode(GameCarrier.Clients.GcClientMode mode)
        {
            GameCarrier.Clients.ClientBase.Manager.Init(mode);
        }

        public static void InitClientLibraryLogging(string logFilePath, GameCarrier.Common.LogLevel logLevel = GameCarrier.Common.LogLevel.LLL_NORMAL)
        {
            GameCarrier.Clients.Logger.SetLogOptions(logLevel, logFilePath, GameCarrier.Common.LogFlags.LOG_ALL);
        }

        public static void CleanupClientLibraryMode()
        {
            GameCarrier.Clients.ClientBase.Manager.Cleanup();
        }

        // TODO: set keep-alive timeout here into Gc core

        #endregion

        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<ServiceConnect>();

        class ClientAdapter : GameCarrier.Clients.GcClient
        {
            public ClientAdapter(string address, string appName) : base(address, appName) { }
        }
    }

    public partial class ServiceConnect<C> : ServiceConnect where C : ServiceClientBase, new() { }
}

namespace LoadBalancer
{
    public static partial class ServiceFactoryExtensions { }
}
