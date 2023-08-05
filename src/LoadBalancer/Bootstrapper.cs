using LoadBalancer.Common;
using static LoadBalancer.Extensions.Serialization;

namespace LoadBalancer
{
    public static class Bootstrapper
    {
        public static class DataTypes
        {
            public static readonly DataType Point3f = 101;
            public static readonly DataType Point2f = 102;
        }

        public static void RegisterTypes()
        {
            CreateSerializer(DataTypes.Point3f,
                r =>
                {
                    var x = r.ReadSingle();
                    var y = r.ReadSingle();
                    var z = r.ReadSingle();
                    return new Point3f(x, y, z);
                },
                (w, o) => { w.Write(o.X); w.Write(o.Y); w.Write(o.Z); })
                .RegisterSerializer();

            CreateSerializer(DataTypes.Point2f,
                r =>
                {
                    var x = r.ReadSingle();
                    var y = r.ReadSingle();
                    return new Point2f(x, y);
                },
                (w, o) => { w.Write(o.X); w.Write(o.Y); })
                .RegisterSerializer();
        }
    }
}
