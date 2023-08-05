namespace LoadBalancer.Server.Common
{
    public class ConnectionInfo
    {
        public string Protocol { get; set; }
        public string RemoteIP { get; set; }
        public int RemotePort { get; set; }
        public string LocalIP { get; set; }
        public int LocalPort { get; set; }
    }
}
