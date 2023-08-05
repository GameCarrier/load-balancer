using LoadBalancer.Common;

namespace LoadBalancer.Game
{
    [ReflectionSerialization]
    public class PublishRoomParameters
    {
        [ReflectionSerialization]
        public class Player
        {
            public string PlayerId { get; set; }
            public KeyValueCollection PlayerProperties { get; set; }
        }

        public string RoomId { get; set; }
        public KeyValueCollection RoomProperties { get; set; }
        public Player[] RoomPlayers { get; set; }
    }
}
