using LoadBalancer.Common;
using LoadBalancer.Game;

namespace LoadBalancer.Server.Jump.Game
{
    public class PlayerDescription : BasePlayer<RoomDescription>
    {
        public void UpdateProperties(KeyValueCollection properties, bool raise)
        {
            Properties.Merge(properties);

            if (raise || SharedSettings.RaiseLocalEvents)
                RaisePropertiesChanged(properties);
        }
    }
}
