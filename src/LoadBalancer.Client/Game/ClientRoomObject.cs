using LoadBalancer.Common;
using LoadBalancer.Game;
using System;

namespace LoadBalancer.Client.Game
{
    partial class ClientRoomObject : BaseRoomObject<ClientRoom>, IClientRoomObject
    {
        public GameServiceClient Service { get; set; }

        public void UpdateProperties(KeyValueCollection properties, bool raise, bool notify = true)
        {
            Properties.ExecuteWithoutTracking(() =>
                Properties.Merge(properties));

            if (raise || SharedSettings.RaiseLocalEvents)
                RaisePropertiesChanged(properties);

            if (Room != null)
                Room.RaiseObjectPropertiesChanged(this, properties);

            if (notify && Room != null && properties.Count > 0)
            {
                var evt = new UpdateObjectParameters { RoomId = Room.RoomId, ObjectId = ObjectId, ObjectProperties = properties };
                Room.Service.UpdateObject(evt);
            }
        }

        public void Destroy(bool raise, bool notify = true)
        {
            if (Room != null)
            {
                var roomId = Room.RoomId;
                var service = Room.Service;

                Room.Objects.Remove(this, raise);

                if (notify)
                {
                    var evt = new DestroyObjectParameters { RoomId = roomId, ObjectId = ObjectId };
                    service.DestroyObject(evt);
                }
            }
        }

        public event Action OnDestroy;
        public void RaiseOnDestroy() => OnDestroy?.Invoke();

        bool IGameObject.IsConnected => Room != null && Room.Service != null;
        KeyValueCollection IGameObject.Properties => base.Properties;
        void IGameObject.UpdateProperties(KeyValueCollection properties) =>
            UpdateProperties(properties, raise: false, notify: true);
        void IClientRoomObject.Destroy() => Destroy(raise: false, notify: true);
    }
}
