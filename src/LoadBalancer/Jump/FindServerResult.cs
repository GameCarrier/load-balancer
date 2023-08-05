using LoadBalancer.Common;

namespace LoadBalancer.Jump
{
    [ReflectionSerialization]
    public class FindServerResult : Result
    {
        public RoomLocator Room { get; set; }
    }
}
