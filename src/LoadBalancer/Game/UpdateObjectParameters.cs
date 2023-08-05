using LoadBalancer.Common;

namespace LoadBalancer.Game
{
    [ReflectionSerialization]
    public class UpdateObjectParameters
    {
        public string RoomId { get; set; }
        public string ObjectId { get; set; }
        public KeyValueCollection ObjectProperties { get; set; }
    }
}
