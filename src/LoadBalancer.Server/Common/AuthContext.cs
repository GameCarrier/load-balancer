using LoadBalancer.Common;

namespace LoadBalancer.Server.Common
{
    public class AuthContext
    {
        public string SessionId { get; set; }
        public KeyValueCollection Claims { get; set; }
        public DateTime LoginDate { get; set; }
    }
}
