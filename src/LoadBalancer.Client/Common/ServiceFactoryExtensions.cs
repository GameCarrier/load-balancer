using LoadBalancer.Client.Common;

namespace LoadBalancer
{
    static partial class ServiceFactoryExtensions
    {
        public static void RegisterConnect<I, T>(this ServiceFactory factory) where T : ServiceClientBase, I, new() =>
            factory.Register<IServiceConnect<I>, ServiceConnect<T>>();

        public static IServiceConnect<I> GetConnect<I>(this ServiceFactory factory) =>
            factory.Get<IServiceConnect<I>>();
    }
}
