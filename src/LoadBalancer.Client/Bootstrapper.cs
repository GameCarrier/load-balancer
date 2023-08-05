using LoadBalancer.Auth;
using LoadBalancer.Client.Auth;
using LoadBalancer.Client.Game;
using LoadBalancer.Client.Jump;
using LoadBalancer.Game;
using LoadBalancer.Jump;

namespace LoadBalancer.Client
{
    public static class Bootstrapper
    {
        public static void RegisterTypes()
        {
            LoadBalancer.Bootstrapper.RegisterTypes();
        }

        public static void ConfigureServiceFactory(ServiceFactory factory)
        {
            factory.Register<ILogger>(name => new Common.ClientLogger(name));

            factory.RegisterConnect<IAuthServiceClient, AuthServiceClient>();
            factory.RegisterConnect<IJumpServiceClient, JumpServiceClient>();
            factory.RegisterConnect<IGameServiceClient, GameServiceClient>();
        }
    }
}
