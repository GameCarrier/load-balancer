using LoadBalancer.Common;
using LoadBalancer.Game;

namespace LoadBalancer.Server.Jump.Game
{
    public class RoomDescription : BaseRoom<PlayerDescription, BaseRoomObject>
    {
        public Endpoint ServiceEndpoint { get; set; }

        public void UpdateProperties(KeyValueCollection properties, bool raise)
        {
            Properties.Merge(properties);

            if (raise || SharedSettings.RaiseLocalEvents)
                RaisePropertiesChanged(properties);
        }
    }
}
