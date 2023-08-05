using LoadBalancer;
using LoadBalancer.Server.Auth.Jump;
using LoadBalancer.Server.Jump.Game;

namespace LoadBalancer.Server
{
    public class Bootstrapper
    {
        public const int Root = 1;

        public static void RegisterTypes()
        {
            LoadBalancer.Bootstrapper.RegisterTypes();
        }

        public static void ConfigureServiceFactory(ServiceFactory factory)
        {
            factory.Register<ILogger>(name => new Common.ServerLogger(name));

            factory.RegisterConnect<IAuthForJumpServiceClient, AuthForJumpServiceClient>();
            factory.RegisterConnect<IJumpForGameServiceClient, JumpForGameServiceClient>();
        }
    }
}

public class EntryPoint
{
    public EntryPoint()
    {
        LoadBalancer.Server.Bootstrapper.RegisterTypes();
        LoadBalancer.Server.Bootstrapper.ConfigureServiceFactory(ServiceFactory.Instance);
    }
}