using LoadBalancer.Common;
using LoadBalancer.Game;

namespace LoadBalancer.Server.Game
{
    public partial class ServerRoomObject
    {
        partial void FilterPropertiesOnNotify(KeyValueCollection properties)
        {
            if (!properties.ContainsKey(RoomObjectKeys.HostId))
            {
                properties.Remove(RoomObjectKeys.Velocity);
                properties.Remove(RoomObjectKeys.AngularVelocity);
            }
        }
    }
}
