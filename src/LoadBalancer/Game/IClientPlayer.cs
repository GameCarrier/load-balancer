using System;

namespace LoadBalancer.Game
{
    public interface IClientPlayer : IGameObject
    {
        string PlayerId { get; }
        bool IsMyPlayer { get; }
        new PlayerProperties Properties { get; }

        event Action OnLeave;
    }
}
