using GameCarrier.Adapter;
using Microsoft.Extensions.Configuration;

namespace LoadBalancer.Server.Common
{
    public abstract class ServiceBase : GcApplicationBase
    {
        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<ServiceBase>();

        private readonly object lockObject = new object();
        private bool isStopping;
        protected bool IsStopping
        {
            get { lock (lockObject) return isStopping; }
            private set { lock (lockObject) isStopping = value; }
        }

        public readonly IServerThread Thread = new ServerThread();

        public HandlerList Handlers { get; private set; } = new HandlerList();

        protected abstract HandlerBase CreateHandler();

        public override GcConnectionBase OnConnect(IntPtr connenctionHandle, GameCarrier.Adapter.ConnectionInfo info)
        {
            var handler = CreateHandler();

            // Configure handler
            handler.Service = this;
            handler.ConnectionInfo = new ConnectionInfo
            {
                Protocol = info.Protocol.ToString(),
                RemoteIP = info.RemoteIP,
                RemotePort = info.RemotePort,
                LocalIP = info.LocalIP,
                LocalPort = info.LocalPort,
            };
            handler.Configure(connenctionHandle);

            // Register handler in HandlerList
            Handlers.AddHandler(handler);
            handler.Disconnected += () => Handlers.RemoveHandler(handler);

            return handler.Connection;
        }

        public override void OnInit()
        {
            Logger.LogInformation($"OnInit #{Id} {ApplicationName}");
        }

        public override void OnStart()
        {
            Logger.LogInformation($"OnStart #{Id} {ApplicationName}");
            ((ServerThread)Thread).Start();
        }

        public override void OnStop()
        {
            Logger.LogInformation($"OnStop #{Id} {ApplicationName}");
            IsStopping = true;
            ((ServerThread)Thread).Stop();
        }

        public override void OnShutdown()
        {
            Logger.LogInformation("OnShutdown");
        }

        #region Settings

        private IConfigurationRoot ConfigurationRoot;

        protected T ReadConfigurationSection<T>(string section = null)
        {
            if (ConfigurationRoot == null)
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(ApplicationRootPath)
                    .AddJsonFile($"settings_{ApplicationName}.json", optional: true, reloadOnChange: true);

                ConfigurationRoot = builder.Build();
            }

            var node = ConfigurationRoot.GetSection(section ?? typeof(T).Name);
            return node.Get<T>();
        }

        #endregion
    }
}
