using LoadBalancer.Common;

namespace LoadBalancer.Game
{
    [ReflectionSerialization]
    public class LeaveRoomParameters
    {
        public string RoomId { get; set; }
        public string PlayerId { get; set; }
    }
}
