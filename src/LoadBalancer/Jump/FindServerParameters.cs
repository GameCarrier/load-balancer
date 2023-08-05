using LoadBalancer.Common;

namespace LoadBalancer.Jump
{
    [ReflectionSerialization]
    public class FindServerParameters
    {
        public string TitleId { get; set; }
        public string Version { get; set; }
        public KeyValueCollection RoomProperties { get; set; }
    }
}
