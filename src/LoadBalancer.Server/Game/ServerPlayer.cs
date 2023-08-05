using LoadBalancer.Common;
using LoadBalancer.Game;

namespace LoadBalancer.Server.Game
{
    public partial class ServerPlayer : BasePlayer<ServerRoom>//, IGameObject
    {
        public GameServiceHandler Handler { get; set; }

        public new PlayerProperties Properties { get => (PlayerProperties)base.Properties; }

        protected override BasePlayerProperties BuildProperties() => new PlayerProperties();

        public void UpdateProperties(KeyValueCollection properties, bool raise, IEnumerable<ServerPlayer> playersToNotify)
        {
            Properties.Merge(properties);

            if (raise || SharedSettings.RaiseLocalEvents)
                RaisePropertiesChanged(properties);

            if (Room != null)
            {
                Room.RaisePlayerPropertiesChanged(this, properties);

                if (properties.Count > 0)
                {
                    var evt = new UpdatePlayerParameters { RoomId = Room.RoomId, PlayerId = PlayerId, PlayerProperties = properties };
                    Notify(playersToNotify, p => p.Handler.OnPlayerUpdated(evt));
                }
            }
        }

        public void RaiseRoomEvent(KeyType name, KeyValueCollection parameters, IEnumerable<ServerPlayer> playersToNotify)
        {
            if (Room != null)
            {
                Notify(playersToNotify, p =>
                {
                    var evt = new RoomEvent { SenderId = PlayerId, RoomId = Room.RoomId, PlayerId = p.PlayerId, Name = name, Parameters = parameters };
                    p.Handler.OnRoomEventRaised(evt);
                });
            }
        }

        protected void Notify(IEnumerable<ServerPlayer> players, Action<ServerPlayer> action)
        {
            if (players == null)
                players = Room.Players;

            foreach (var recipient in players)
                action(recipient);
        }

        //bool IGameObject.IsConnected => Room != null;
        //KeyValueCollection IGameObject.Properties => Properties;
        //void IGameObject.UpdateProperties(KeyValueCollection properties) => UpdateProperties(properties, Room.Players.None);
    }
}
