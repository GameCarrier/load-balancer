using LoadBalancer.Server.Common;

namespace LoadBalancer.Server.Test
{
    internal class TestService : ServiceBase
    {
        protected override HandlerBase CreateHandler() => new TestServiceHandler();
    }
}
