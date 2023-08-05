namespace LoadBalancer.Server.Common
{
    public class CryptographySettings
    {
        public string TokenSalt { get; set; }
        public string SharedSecret { get; set; }
        public float TokenTtl { get; set; }
    }
}
