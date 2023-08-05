using LoadBalancer.Common;
using LoadBalancer.Game;
using System;

namespace LoadBalancer.Client.Game
{
    partial class ClientRoom : BaseRoom<ClientPlayer, ClientRoomObject>, IClientRoom
    {
        public GameServiceClient Service { get; set; }

        public new RoomProperties Properties { get => (RoomProperties)base.Properties; }
        protected override BaseRoomProperties BuildProperties() => new RoomProperties();

        IPlayerList<IClientPlayer> IClientRoom.Players => Players;
        IRoomObjectList<IClientRoomObject> IClientRoom.Objects => Objects;

        event Action<IClientPlayer, KeyValueCollection> IClientRoom.OnPlayerPropertiesChanged
        {
            add => OnPlayerPropertiesChanged += value;
            remove => OnPlayerPropertiesChanged -= value;
        }

        public void UpdateProperties(KeyValueCollection properties, bool raise, bool notify = true)
        {
            Properties.ExecuteWithoutTracking(() =>
                Properties.Merge(properties));

            if (raise || SharedSettings.RaiseLocalEvents)
                RaisePropertiesChanged(properties);

            if (notify && properties.Count > 0)
            {
                var evt = new UpdateRoomParameters { RoomId = RoomId, RoomProperties = properties };
                Service.UpdateRoom(evt);
            }
        }

        public void RaiseRoomEvent(KeyType name, KeyValueCollection parameters, string recipientId = null)
        {
            var evt = new RoomEvent { RoomId = RoomId, PlayerId = recipientId, Name = name, Parameters = parameters };
            Service.RaiseRoomEvent(evt);
        }

        bool IGameObject.IsConnected => Service != null;
        KeyValueCollection IGameObject.Properties => base.Properties;
        void IGameObject.UpdateProperties(KeyValueCollection changes) =>
            UpdateProperties(changes, raise: false, notify: true);
    }
}
