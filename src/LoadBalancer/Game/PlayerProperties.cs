using LoadBalancer.Common;

namespace LoadBalancer.Game
{
    public class PlayerProperties : BasePlayerProperties
    {
        public PlayerProperties() { }
        public PlayerProperties(KeyValueCollection source) : base(source) { }

        public bool IsHost
        {
            get => GetValue<bool>(PlayerKeys.IsHost);
            set => SetValue(PlayerKeys.IsHost, value);
        }

        public int Level
        {
            get => GetValue<int>(PlayerKeys.Level);
            set => SetValue(PlayerKeys.Level, value);
        }

        public Point3f Position
        {
            get => GetValue<Point3f>(PlayerKeys.Position);
            set => SetValue(PlayerKeys.Position, value);
        }

        public Point3f Rotation
        {
            get => GetValue<Point3f>(PlayerKeys.Rotation);
            set => SetValue(PlayerKeys.Rotation, value);
        }

        public Point2f MoveDirection
        {
            get => GetValue<Point2f>(PlayerKeys.MoveDirection);
            set => SetValue(PlayerKeys.MoveDirection, value);
        }

        public bool IsSprint
        {
            get => GetValue<bool>(PlayerKeys.IsSprint);
            set => SetValue(PlayerKeys.IsSprint, value);
        }

        public bool IsJump
        {
            get => GetValue<bool>(PlayerKeys.IsJump);
            set => SetValue(PlayerKeys.IsJump, value);
        }
    }
}
