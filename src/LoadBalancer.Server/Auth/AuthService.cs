using LoadBalancer.Server.Common;

namespace LoadBalancer.Server.Auth
{
    public class AuthService : ServiceBase
    {
        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<AuthService>();

        public AuthServiceSettings Settings { get; private set; }

        public override void OnStart()
        {
            base.OnStart();
            Settings = ReadConfigurationSection<AuthServiceSettings>();
            AuthTokenUtils.CryptographySettings = ReadConfigurationSection<CryptographySettings>();
        }

        protected override HandlerBase CreateHandler() => new AuthServiceHandler();
    }
}
