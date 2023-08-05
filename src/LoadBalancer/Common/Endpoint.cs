using System;

namespace LoadBalancer.Common
{
    [ReflectionSerialization]
    public class Endpoint
    {
        public static Endpoint Parse(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            var uri = new Uri(address);
            return new Endpoint(uri.Scheme, uri.Host, uri.Port, uri.AbsolutePath.TrimStart('/'));
        }

        public Endpoint() { }

        public Endpoint(string protocol, string address, int port, string appName)
        {
            Protocol = protocol;
            Address = address;
            Port = port;
            AppName = appName;
        }

        public string Protocol { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public string AppName { get; set; }

        public override string ToString() => $"{Protocol}://{Address}:{Port}/{AppName}";

        public override int GetHashCode() => ToString().ToLower().GetHashCode();

        public override bool Equals(object obj) => obj is Endpoint other
            && ToString().ToLower().Equals(other.ToString().ToLower());
    }
}
