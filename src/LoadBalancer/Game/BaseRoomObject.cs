using LoadBalancer.Common;
using System;

namespace LoadBalancer.Game
{
    public class BaseRoomObject
    {
        public string ObjectId { get; set; }
        public string Tag { get; set; }
        public BaseRoom Room { get; set; }

        public RoomObjectProperties Properties { get; private set; }
        public event Action<KeyValueCollection> OnPropertiesChanged;

        public BaseRoomObject()
        {
            Properties = BuildProperties();
        }

        protected virtual RoomObjectProperties BuildProperties() => new RoomObjectProperties();

        protected void RaisePropertiesChanged(KeyValueCollection properties) =>
            OnPropertiesChanged?.Invoke(properties);
    }

    public class BaseRoomObject<R> : BaseRoomObject where R : BaseRoom
    {
        public new R Room { get => (R)base.Room; set => base.Room = value; }

        public override string ToString() =>
            $"#{ObjectId}, props: {Properties}";
    }
}
