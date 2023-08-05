namespace LoadBalancer.Server.Jump
{
    public class JumpServiceSettings
    {
        public string Region { get; set; }
        public string TitleId { get; set; }
        public string Version { get; set; }

        public string[] AuthServiceEndpoints { get; set; }
        public string PublicServiceEndpoint { get; set; }
    }
}
