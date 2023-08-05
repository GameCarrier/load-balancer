using LoadBalancer.Common;

namespace LoadBalancer.Server.Jump.Game
{
    [ReflectionSerialization]
    public class AddGameServiceParameters
    {
        public Endpoint ServiceEndpoint { get; set; }
        public KeyValueCollection ServiceProperties { get; set; }
    }
}
