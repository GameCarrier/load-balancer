using LoadBalancer.Common;

namespace LoadBalancer.Auth
{
    [ReflectionSerialization]
    public class ListJumpServicesParameters
    {
        public string Region { get; set; }
        public string TitleId { get; set; }
        public string Version { get; set; }
    }
}
