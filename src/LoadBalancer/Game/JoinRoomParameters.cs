using LoadBalancer.Common;

namespace LoadBalancer.Game
{
    [ReflectionSerialization]
    public class JoinRoomParameters
    {
        public string RoomId { get; set; }
        public string PlayerId { get; set; }
        public KeyValueCollection PlayerProperties { get; set; }
    }
}
