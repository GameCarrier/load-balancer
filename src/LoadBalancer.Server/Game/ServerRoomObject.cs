using LoadBalancer.Common;
using LoadBalancer.Game;

namespace LoadBalancer.Server.Game
{
    public partial class ServerRoomObject : BaseRoomObject<ServerRoom>// , IGameObject
    {
        partial void FilterPropertiesOnNotify(KeyValueCollection properties);

        public void UpdateProperties(KeyValueCollection properties, bool raise, IEnumerable<ServerPlayer> playersToNotify)
        {
            Properties.Merge(properties);

            if (raise || SharedSettings.RaiseLocalEvents)
                RaisePropertiesChanged(properties);

            if (Room != null)
            {
                Room.RaiseObjectPropertiesChanged(this, properties);

                FilterPropertiesOnNotify(properties);

                if (properties.Count > 0)
                {
                    var evt = new UpdateObjectParameters { RoomId = Room.RoomId, ObjectId = ObjectId, ObjectProperties = properties };
                    Notify(playersToNotify, p => p.Handler.OnObjectUpdated(evt));
                }
            }
        }

        public void DestroyObject(bool raise, IEnumerable<ServerPlayer> playersToNotify)
        {
            if (Room != null)
            {
                var roomId = Room.RoomId;

                Room.Objects.Remove(this, raise);

                var evt = new DestroyObjectParameters { RoomId = roomId, ObjectId = ObjectId };
                Notify(playersToNotify, p => p.Handler.OnObjectDestroyed(evt));
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
        //void IGameObject.UpdateProperties(KeyValueCollection properties) => UpdateProperties(properties, Room.Players);
    }
}
