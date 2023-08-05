using LoadBalancer.Game;

namespace LoadBalancer.Server.Game
{
    public partial class GameService
    {
        public RoomList<ServerRoom> Rooms { get; private set; } = new RoomList<ServerRoom>();

        private void InitializeRoomLogic()
        {
            Rooms.OnRoomAdded += Rooms_OnRoomAdded;
        }

        private void Rooms_OnRoomAdded(ServerRoom room)
        {
            // Add/Override room properties on Room create
            room.Properties.MaxPlayers = 10;

            room.Players.OnJoin += Players_OnJoin;
            room.Players.OnLeave += Players_OnLeave;
        }

        private void Players_OnJoin(ServerPlayer player)
        {
            // Add/Override room properties on Player join
            player.Properties.UserId = player.Properties.Nickname;
            player.Properties.Level = 10;
        }

        private void Players_OnLeave(ServerPlayer player)
        {
            if (player.Room.IsEmpty) return;

            var newHost = player.Room.Players.First();

            // Choose new Host
            if (player.Properties.IsHost)
            {
                newHost.Properties.IsHost = true;
                newHost.UpdateProperties(new PlayerProperties { IsHost = true }, raise: true, playersToNotify: player.Room.Players);
            }

            // Destroy owner RoomObjects
            foreach (var obj in player.Room.Objects.Where(o => o.Properties.OwnerId == player.PlayerId))
                obj.DestroyObject(raise: true, playersToNotify: player.Room.Players);

            // Rehost hosted RoomObjects
            foreach (var obj in player.Room.Objects.Where(o => o.Properties.HostId == player.PlayerId))
                obj.UpdateProperties(new RoomObjectProperties 
                { 
                    HostId = newHost.PlayerId,
                    Velocity = obj.Properties.Velocity,
                    AngularVelocity = obj.Properties.AngularVelocity,
                }, raise: true, playersToNotify: player.Room.Players);
        }
    }
}
