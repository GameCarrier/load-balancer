using LoadBalancer.Common;

namespace LoadBalancer.Game
{
    [ReflectionSerialization]
    public class JoinRoomResult : Result
    {
        [ReflectionSerialization]
        public class Object
        {
            public string ObjectId { get; set; }
            public string Tag { get; set; }
            public KeyValueCollection ObjectProperties { get; set; }
        }

        [ReflectionSerialization]
        public class Player
        {
            public string PlayerId { get; set; }
            public KeyValueCollection PlayerProperties { get; set; }
        }

        public string RoomId { get; set; }
        public KeyValueCollection RoomProperties { get; set; }
        public Object[] RoomObjects { get; set; }
        public Player[] RoomPlayers { get; set; }

        public string PlayerId { get; set; }
        public KeyValueCollection PlayerProperties { get; set; }
    }
}
