using LoadBalancer.Common;
using LoadBalancer.Game;

namespace LoadBalancer.Server.Jump.Game
{
    public class GameServiceState
    {
        public JumpServiceHandler Handler { get; set; }
        public Endpoint ServiceEndpoint { get; set; }
        public GameServiceProperties ServiceProperties { get; private set; } = new GameServiceProperties();
        public RoomList<RoomDescription> Rooms { get; private set; } = new RoomList<RoomDescription>();
    }
}
