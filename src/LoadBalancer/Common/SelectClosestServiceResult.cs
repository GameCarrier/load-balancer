namespace LoadBalancer.Common
{
    [ReflectionSerialization]
    public class SelectClosestServiceResult : Result
    {
        public Endpoint ServiceEndpoint { get; set; }
    }
}
