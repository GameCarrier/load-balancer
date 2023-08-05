using LoadBalancer.Common;
using LoadBalancer.Game;
using System;

namespace LoadBalancer.Client.Game
{
    partial class ClientPlayer : BasePlayer<ClientRoom>, IClientPlayer
    {
        public bool IsMyPlayer { get; set; }
        public new PlayerProperties Properties { get => (PlayerProperties)base.Properties; }
        protected override BasePlayerProperties BuildProperties() => new PlayerProperties();

        public void UpdateProperties(KeyValueCollection properties, bool raise, bool notify = true)
        {
            if (notify && PlayerId != Room.Service.Player.PlayerId)
                throw new ResultException(GameErrors.Error_PlayerNotFound);

            Properties.ExecuteWithoutTracking(() =>
                Properties.Merge(properties));

            if (raise || SharedSettings.RaiseLocalEvents)
                RaisePropertiesChanged(properties);

            if (Room != null)
            {
                Room.RaisePlayerPropertiesChanged(this, properties);

                if (notify && properties.Count > 0)
                {
                    var evt = new UpdatePlayerParameters { RoomId = Room.RoomId, PlayerId = PlayerId, PlayerProperties = properties };
                    Room.Service.UpdatePlayer(evt);
                }
            }
        }

        public event Action OnLeave;
        public void RaiseOnLeave() => OnLeave?.Invoke();

        bool IGameObject.IsConnected => Room != null;
        KeyValueCollection IGameObject.Properties => base.Properties;
        void IGameObject.UpdateProperties(KeyValueCollection changes) =>
            UpdateProperties(changes, raise: false, notify: true);
    }
}
