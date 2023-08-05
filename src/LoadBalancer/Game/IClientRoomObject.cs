using System;

namespace LoadBalancer.Game
{
    public interface IClientRoomObject : IGameObject
    {
        string ObjectId { get; }
        string Tag { get; }
        new RoomObjectProperties Properties { get; }
        void Destroy();

        event Action OnDestroy;
    }
}
