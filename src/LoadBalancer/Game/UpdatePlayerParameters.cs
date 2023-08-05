using LoadBalancer.Common;

namespace LoadBalancer.Game
{
    [ReflectionSerialization]
    public class UpdatePlayerParameters
    {
        public string RoomId { get; set; }
        public string PlayerId { get; set; }
        public KeyValueCollection PlayerProperties { get; set; }
    }
}
