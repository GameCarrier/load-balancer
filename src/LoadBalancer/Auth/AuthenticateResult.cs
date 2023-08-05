using LoadBalancer.Common;

namespace LoadBalancer.Auth
{
    [ReflectionSerialization]
    public class AuthenticateResult : Result
    {
        public string AuthToken { get; set; }
    }
}
