using static System.Math;

namespace LoadBalancer.Common
{
    public class Point2f
    {
        public const float kEpsilon = 0.0001f;
        public Point2f() { }

        public Point2f(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; set; }
        public float Y { get; set; }

        public override bool Equals(object obj) => obj is Point2f other && Distance(other) <= kEpsilon;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => $"({X}, {Y})";

        public float Distance(Point2f other) => (float)Sqrt(Pow(X - other.X, 2) + Pow(Y - other.Y, 2));
    }
}
