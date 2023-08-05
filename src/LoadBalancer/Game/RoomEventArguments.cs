using LoadBalancer.Common;

namespace LoadBalancer.Game
{
    public class RoomEventArguments : KeyValueCollection
    {
        public RoomEventArguments() { }
        public RoomEventArguments(KeyValueCollection source) : base(source) { }

        public string ObjectId
        {
            get => GetValue<string>(RoomEventKeys.ObjectId);
            set => SetValue(RoomEventKeys.ObjectId, value);
        }

        public Point3f Force
        {
            get => GetValue<Point3f>(RoomEventKeys.Force);
            set => SetValue(RoomEventKeys.Force, value);
        }

        public int Times
        {
            get => GetValue<int>(RoomEventKeys.Times);
            set => SetValue(RoomEventKeys.Times, value);
        }

        public float Interval
        {
            get => GetValue<float>(RoomEventKeys.Interval);
            set => SetValue(RoomEventKeys.Interval, value);
        }
    }
}
