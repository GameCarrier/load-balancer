using LoadBalancer.Server.Common;

namespace LoadBalancer.Server.Jump
{
    public partial class JumpService : ServiceBase
    {
        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<JumpService>();

        public JumpServiceSettings Settings { get; private set; }

        public override void OnStart()
        {
            base.OnStart();
            Settings = ReadConfigurationSection<JumpServiceSettings>();
            AuthTokenUtils.CryptographySettings = ReadConfigurationSection<CryptographySettings>();
            SetupAuthServiceConnect();
        }

        protected override HandlerBase CreateHandler() => new JumpServiceHandler();

        public override void OnStop()
        {
            base.OnStop();
            foreach (var connect in AuthServiceConnects)
                connect.Dispose();
        }
    }
}
