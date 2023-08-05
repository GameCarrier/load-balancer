using LoadBalancer.Common;

namespace LoadBalancer.Game
{
    [ReflectionSerialization]
    public class CreateRoomParameters
    {
        [ReflectionSerialization]
        public class Object
        {
            public string ObjectId { get; set; }
            public string Tag { get; set; }
            public KeyValueCollection ObjectProperties { get; set; }
        }

        public string RoomId { get; set; }
        public KeyValueCollection RoomProperties { get; set; }
        public string PlayerId { get; set; }
        public KeyValueCollection PlayerProperties { get; set; }
        public Object[] RoomObjects { get; set; }
    }
}
