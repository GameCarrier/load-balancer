using LoadBalancer.Common;

namespace LoadBalancer.Server.Jump.Game
{
    [ReflectionSerialization]
    public class UpdateGameServiceParameters
    {
        public KeyValueCollection ServiceProperties { get; set; }
    }
}
