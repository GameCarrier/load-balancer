using LoadBalancer.Common;
using System.Collections.Generic;

namespace LoadBalancer.Game
{
    public class BasePlayerProperties : KeyValueCollection
    {
        public BasePlayerProperties() { }
        public BasePlayerProperties(KeyValueCollection source) : base(source) { }

        public string UserId
        {
            get => GetValue<string>(PlayerKeys.UserId);
            set => SetValue(PlayerKeys.UserId, value);
        }

        public string Nickname
        {
            get => GetValue<string>(PlayerKeys.Nickname);
            set => SetValue(PlayerKeys.Nickname, value);
        }

        public override IEnumerable<KeyType> SupportedKeys => new KeyType[]
        {
            PlayerKeys.UserId,
            PlayerKeys.Nickname,
        };
    }
}
