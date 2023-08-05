namespace LoadBalancer.Common
{
    [ReflectionSerialization]
    public class PingResult : Result
    {
        public int PingMiliseconds { get; set; }
    }
}
