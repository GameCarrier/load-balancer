using LoadBalancer.Common;
using System.Collections.Generic;

namespace LoadBalancer.Game
{
    public class BaseRoomProperties : KeyValueCollection
    {
        public BaseRoomProperties() { }
        public BaseRoomProperties(KeyValueCollection source) : base(source) { }

        public int MaxPlayers
        {
            get => GetValue<int>(RoomKeys.MaxPlayers);
            set => SetValue(RoomKeys.MaxPlayers, value);
        }

        public bool IsPrivate
        {
            get => GetValue<bool>(RoomKeys.IsPrivate);
            set => SetValue(RoomKeys.IsPrivate, value);
        }

        public string SceneName
        {
            get => GetValue<string>(RoomKeys.SceneName);
            set => SetValue(RoomKeys.SceneName, value);
        }

        public override IEnumerable<KeyType> SupportedKeys => new KeyType[]
        {
            RoomKeys.MaxPlayers,
            RoomKeys.IsPrivate,
            RoomKeys.SceneName,
        };
    }
}
