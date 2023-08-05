using LoadBalancer.Common;

namespace LoadBalancer.Game
{
    [ReflectionSerialization]
    public class UpdateRoomParameters
    {
        public string RoomId { get; set; }
        public KeyValueCollection RoomProperties { get; set; }
    }
}
