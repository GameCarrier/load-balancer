using LoadBalancer.Common;
using System;

namespace LoadBalancer.Game
{
    public interface IClientRoom : IGameObject
    {
        string RoomId { get; }
        new RoomProperties Properties { get; }
        IPlayerList<IClientPlayer> Players { get; }
        IRoomObjectList<IClientRoomObject> Objects { get; }

        event Action<IClientPlayer, KeyValueCollection> OnPlayerPropertiesChanged;

        event Action<RoomEvent> OnEventReceived;
        void RaiseRoomEvent(KeyType name, KeyValueCollection parameters, string recipientId = null);
    }
}
