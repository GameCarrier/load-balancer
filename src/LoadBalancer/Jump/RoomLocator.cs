using LoadBalancer.Common;

namespace LoadBalancer.Jump
{
    [ReflectionSerialization]
    public class RoomLocator
    {
        public Endpoint ServiceEndpoint { get; set; }
        public string RoomId { get; set; }
        public KeyValueCollection RoomProperties { get; set; }
        public bool IsExistingRoom { get; set; }

        public override string ToString() => $"{RoomId} at {ServiceEndpoint}";
    }
}
