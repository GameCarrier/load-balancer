using LoadBalancer.Common;
using System;

namespace LoadBalancer.Game
{
    public class BaseRoom
    {
        public string RoomId { get; set; }
        public BaseRoomProperties Properties { get; private set; }
        public event Action<KeyValueCollection> OnPropertiesChanged;

        public event Action<RoomEvent> OnEventReceived;

        public BaseRoom()
        {
            Properties = BuildProperties();
        }

        protected virtual BaseRoomProperties BuildProperties() => new BaseRoomProperties();

        protected void RaisePropertiesChanged(KeyValueCollection properties) =>
            OnPropertiesChanged?.Invoke(properties);

        public void RaiseEventReceived(RoomEvent evt) =>
            OnEventReceived?.Invoke(evt);
    }

    public class BaseRoom<P, O> : BaseRoom where P : BasePlayer where O : BaseRoomObject
    {
        public PlayerList<P> Players { get; private set; } = new PlayerList<P>();
        public RoomObjectList<O> Objects { get; private set; } = new RoomObjectList<O>();
        public bool IsEmpty => Players.Count == 0;

        public event Action<P, KeyValueCollection> OnPlayerPropertiesChanged;
        public event Action<O, KeyValueCollection> OnObjectPropertiesChanged;

        public BaseRoom()
        {
            Objects.Room = this;
            Players.Room = this;
        }

        public void RaisePlayerPropertiesChanged(P player, KeyValueCollection properties) =>
            OnPlayerPropertiesChanged?.Invoke(player, properties);

        public void RaiseObjectPropertiesChanged(O obj, KeyValueCollection properties) =>
            OnObjectPropertiesChanged?.Invoke(obj, properties);

        public override string ToString() =>
            $"#{RoomId}, props: {Properties}, {Players.Count} players";
    }
}
