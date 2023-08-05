using LoadBalancer.Common;

namespace LoadBalancer.Game
{
    public class RoomObjectProperties : KeyValueCollection
    {
        public RoomObjectProperties() { }
        public RoomObjectProperties(KeyValueCollection source) : base(source) { }

        public string CreatorId
        {
            get => GetValue<string>(RoomObjectKeys.CreatorId);
            set => SetValue(RoomObjectKeys.CreatorId, value);
        }

        public string OwnerId
        {
            get => GetValue<string>(RoomObjectKeys.OwnerId);
            set => SetValue(RoomObjectKeys.OwnerId, value);
        }

        public string HostId
        {
            get => GetValue<string>(RoomObjectKeys.HostId);
            set => SetValue(RoomObjectKeys.HostId, value);
        }

        public string Name
        {
            get => GetValue<string>(RoomObjectKeys.Name);
            set => SetValue(RoomObjectKeys.Name, value);
        }

        public Point3f Position
        {
            get => GetValue<Point3f>(RoomObjectKeys.Position);
            set => SetValue(RoomObjectKeys.Position, value);
        }

        public Point3f Rotation
        {
            get => GetValue<Point3f>(RoomObjectKeys.Rotation);
            set => SetValue(RoomObjectKeys.Rotation, value);
        }

        public Point3f Velocity
        {
            get => GetValue<Point3f>(RoomObjectKeys.Velocity);
            set => SetValue(RoomObjectKeys.Velocity, value);
        }

        public Point3f AngularVelocity
        {
            get => GetValue<Point3f>(RoomObjectKeys.AngularVelocity);
            set => SetValue(RoomObjectKeys.AngularVelocity, value);
        }
    }
}
