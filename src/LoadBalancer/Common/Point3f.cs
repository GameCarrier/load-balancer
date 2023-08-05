using static System.Math;

namespace LoadBalancer.Common
{
    public class Point3f
    {
        public const float kEpsilon = 0.0001f;
        public Point3f() { }

        public Point3f(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public override bool Equals(object obj) => obj is Point3f other && Distance(other) <= kEpsilon;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => $"({X}, {Y}, {Z})";

        public float Distance(Point3f other) => (float)Sqrt(Pow(X - other.X, 2) + Pow(Y - other.Y, 2) + Pow(Z - other.Z, 2));
    }
}
