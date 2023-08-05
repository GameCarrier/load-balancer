using LoadBalancer.Common;
using LoadBalancer.Extensions;

namespace LoadBalancer.Tests
{
    [ReflectionSerialization]
    public class TestComplexResult : Result
    {
        // public DateTimeOffset Offset { get; set; }
        public bool Bool { get; set; }
        public char Char { get; set; }
        public int Int { get; set; }
        public float Float { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
        public string String { get; set; }

        public Endpoint ServiceEndpoint { get; set; }
        public Endpoint[] Endpoints { get; set; }
        public KeyValueCollection Map { get; set; }

        public Compressed<Endpoint> ZipEndpoint { get; set; }
        public Compressed<Endpoint[]> ZipEndpoints { get; set; }
        public Compressed<KeyValueCollection> ZipMap { get; set; }
        public Compressed<KeyValueCollection[]> ZipMapArray { get; set; }
    }
}
