using LoadBalancer.Common;

namespace LoadBalancer.Game
{
    [ReflectionSerialization]
    public class DestroyObjectParameters
    {
        public string RoomId { get; set; }
        public string ObjectId { get; set; }
    }
}
