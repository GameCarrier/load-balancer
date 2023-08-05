using LoadBalancer.Client.Common;
using LoadBalancer.Common;
using LoadBalancer.Server.Common;
using LoadBalancer.Server.Jump.Game;

namespace LoadBalancer.Server.Game
{
    public partial class GameService
    {
        private IServiceConnect<IJumpForGameServiceClient> JumpServiceConnect;
        private IScheduledItem SchedulerJumpServiceConnect;
        private IScheduledItem SchedulerUpdateGameService;
        internal IJumpForGameServiceClient JumpService => JumpServiceConnect.Service;
        internal IServerThread JumpServiceThread => JumpServiceConnect.Thread;


        private const int Timeout_JumpServiceConnect = 200;
        private const int Timeout_JumpServiceReconnect = 5000;
        private const int Interval_UpdateGameService = 10000;

#if DEBUG
        #region Imitate S2S disconnect
        private bool isJumpServiceConnectDisabled;

        public void DisableJumpServiceConnect()
        {
            isJumpServiceConnectDisabled = true;
            _ = JumpServiceConnect.Disconnect();
        }

        public void EnableJumpServiceConnect()
        {
            isJumpServiceConnectDisabled = false;
            if (!JumpServiceConnect.IsConnected)
                SchedulerJumpServiceConnect.Resume(Timeout_JumpServiceConnect);
        }
        #endregion
#endif

        private void SetupJumpServiceConnect()
        {
            JumpServiceConnect = ServiceFactory.Instance.GetConnect<IJumpForGameServiceClient>();
            SchedulerJumpServiceConnect = JumpServiceConnect.Thread.Schedule(async () =>
            {
                if (IsStopping) return;
#if DEBUG
                if (isJumpServiceConnectDisabled) return;
#endif
                Logger.LogInformation($"Establishing connection to Jump {Settings.JumpServiceEndpoint}");
                await JumpServiceConnect.Connect(Endpoint.Parse(Settings.JumpServiceEndpoint));
            }, Timeout_JumpServiceConnect, Timeout_JumpServiceReconnect);

            JumpServiceConnect.OnConnected += () =>
            {
                JumpService.OnGameServiceAdded(new AddGameServiceParameters
                {
                    ServiceEndpoint = Endpoint.Parse(Settings.PublicServiceEndpoint),
                    ServiceProperties = new GameServiceProperties { LoadLevel = LoadLevel },
                });

                foreach (var room in Rooms)
                    RepublishRoom(room);

                Logger.LogInformation($"Registered on Jump {Settings.JumpServiceEndpoint}");
                SchedulerJumpServiceConnect.Suspend();
                SchedulerUpdateGameService.Resume();
            };

            JumpServiceConnect.OnDisconnected += reason =>
            {
                Logger.LogInformation($"Disconnected from {Settings.JumpServiceEndpoint}");
                SchedulerUpdateGameService.Suspend();
#if DEBUG
                if (isJumpServiceConnectDisabled) return;
#endif
                SchedulerJumpServiceConnect.Resume();
            };

            SchedulerUpdateGameService = JumpServiceConnect.Thread.Schedule(() =>
            {
                Logger.LogInformation($"UpdateGameService on Jump: {LoadLevel} (CPU: {AverageCounterCPU.Average}) (Memory: {AverageCounterMemory.Average})");
                JumpService.OnGameServiceUpdated(new UpdateGameServiceParameters
                {
                    ServiceProperties = new GameServiceProperties { LoadLevel = LoadLevel },
                });
            }, Interval_UpdateGameService, Interval_UpdateGameService, suspended: true);
        }

        public bool RepublishRoom(ServerRoom room)
        {
            return room.Thread.EnqueueNew(() =>
            {
                // Take Room snapshot in Room thread
                var parameters = JumpService.CreatePublishRoomParameters(room, room.Players);
                // Publish Room in Jump thread
                JumpServiceThread.EnqueueNew_IfConnected(() => JumpService.OnRoomPublished(parameters));
            });
        }
    }
}
