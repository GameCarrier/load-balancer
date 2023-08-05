using LoadBalancer.Client.Common;
using LoadBalancer.Common;
using LoadBalancer.Server.Auth.Jump;

namespace LoadBalancer.Server.Jump
{
    public partial class JumpService
    {
        private List<IServiceConnect<IAuthForJumpServiceClient>> AuthServiceConnects = new();

        private const int Timeout_AuthServiceConnect = 200;
        private const int Timeout_AuthServiceReconnect = 5000;

        private void SetupAuthServiceConnect()
        {
            foreach (var endpointUri in Settings.AuthServiceEndpoints)
            {
                var connect = ServiceFactory.Instance.GetConnect<IAuthForJumpServiceClient>();
                var endpoint = Endpoint.Parse(endpointUri);

                connect.SchedulerConnect = connect.Thread.Schedule(async () =>
                {
                    if (IsStopping) return;

                    Logger.LogInformation($"Establishing connection to Auth {endpoint}");
                    await connect.Connect(endpoint);
                }, Timeout_AuthServiceConnect, Timeout_AuthServiceReconnect);

                connect.OnConnected += () =>
                {
                    connect.Service.OnJumpServiceAdded(new AddJumpServiceParameters
                    {
                        ServiceEndpoint = Endpoint.Parse(Settings.PublicServiceEndpoint),
                        ServiceProperties = new JumpServiceProperties
                        {
                            Region = Settings.Region,
                            TitleId = Settings.TitleId,
                            Version = Settings.Version,
                        },
                    });
                    Logger.LogInformation($"Registered on Auth {endpoint}");
                    connect.SchedulerConnect.Suspend();
                };

                connect.OnDisconnected += reason =>
                {
                    Logger.LogInformation($"Disconnected from {endpoint}");
                    connect.SchedulerConnect.Resume();
                };

                AuthServiceConnects.Add(connect);
            }
        }
    }
}
