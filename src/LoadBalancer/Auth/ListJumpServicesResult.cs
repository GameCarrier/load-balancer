using LoadBalancer.Common;

namespace LoadBalancer.Auth
{
    [ReflectionSerialization]
    public class ListJumpServicesResult : Result
    {
        public Endpoint[] Endpoints { get; set; }
    }
}
