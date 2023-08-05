using LoadBalancer.Common;

namespace LoadBalancer.Jump
{
    [ReflectionSerialization]
    public class FindRoomResult : Result
    {
        public RoomLocator[] Rooms { get; set; }
    }
}
