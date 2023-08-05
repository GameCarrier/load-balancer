using LoadBalancer.Common;

namespace LoadBalancer.Game
{
    [ReflectionSerialization]
    public class RoomEvent
    {
        public string SenderId { get; set; }
        public string RoomId { get; set; }
        public string PlayerId { get; set; }
        public KeyType Name { get; set; }
        public KeyValueCollection Parameters { get; set; }
    }
}
