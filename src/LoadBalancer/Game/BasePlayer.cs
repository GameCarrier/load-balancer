using LoadBalancer.Common;
using System;

namespace LoadBalancer.Game
{
    public class BasePlayer
    {
        public string PlayerId { get; set; }
        public BaseRoom Room { get; set; }

        public BasePlayerProperties Properties { get; private set; }
        public event Action<KeyValueCollection> OnPropertiesChanged;

        public BasePlayer()
        {
            Properties = BuildProperties();
        }

        protected virtual BasePlayerProperties BuildProperties() => new BasePlayerProperties();

        protected void RaisePropertiesChanged(KeyValueCollection properties) =>
            OnPropertiesChanged?.Invoke(properties);
    }

    public class BasePlayer<R> : BasePlayer where R : BaseRoom
    {
        public new R Room { get => (R)base.Room; set => base.Room = value; }

        public override string ToString() =>
            $"#{PlayerId}, props: {Properties}";
    }
}
