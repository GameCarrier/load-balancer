using LoadBalancer.Common;

namespace LoadBalancer.Game
{
    [ReflectionSerialization]
    public class SpawnObjectParameters
    {
        public string RoomId { get; set; }
        public string ObjectId { get; set; }
        public string Tag { get; set; }
        public KeyValueCollection ObjectProperties { get; set; }
    }
}
