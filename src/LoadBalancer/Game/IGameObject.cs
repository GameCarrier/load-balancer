using LoadBalancer.Common;
using System;

namespace LoadBalancer.Game
{
    public interface IGameObject
    {
        bool IsConnected { get; }
        KeyValueCollection Properties { get; }
        void UpdateProperties(KeyValueCollection changes);
        event Action<KeyValueCollection> OnPropertiesChanged;
    }
}
