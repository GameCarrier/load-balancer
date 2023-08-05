using LoadBalancer.Common;

namespace LoadBalancer.Jump
{
    [ReflectionSerialization]
    public class FindRoomParameters
    {
        public string TitleId { get; set; }
        public string Version { get; set; }
        public string RoomId { get; set; }
        public KeyValueCollection RoomProperties { get; set; }
        public string[] PlayerIds { get; set; }
    }
}
