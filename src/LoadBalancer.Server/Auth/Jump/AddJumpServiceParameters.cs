using LoadBalancer.Common;

namespace LoadBalancer.Server.Auth.Jump
{
    [ReflectionSerialization]
    public class AddJumpServiceParameters
    {
        public Endpoint ServiceEndpoint { get; set; }
        public KeyValueCollection ServiceProperties { get; set; }
    }
}
