using LoadBalancer.Common;
using LoadBalancer.Server.Common;

namespace LoadBalancer.Server.Jump.Game
{
    public class GameServiceProperties : KeyValueCollection
    {
        enum GameServiceKeys : byte
        {
            LoadLevel,
        }

        public SystemLoadLevel LoadLevel
        {
            get => (SystemLoadLevel)GetValue<byte>(GameServiceKeys.LoadLevel);
            set => SetValue(GameServiceKeys.LoadLevel, (byte)value);
        }
    }
}
