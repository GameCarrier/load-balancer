using LoadBalancer.Common;

namespace LoadBalancer.Server.Auth.Jump
{
    public class JumpServiceState
    {
        public AuthServiceHandler Handler { get; set; }
        public Endpoint ServiceEndpoint { get; set; }
        public JumpServiceProperties ServiceProperties { get; private set; } = new JumpServiceProperties();
    }
}
