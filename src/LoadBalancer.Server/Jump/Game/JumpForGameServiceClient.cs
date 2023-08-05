using LoadBalancer.Client.Common;
using LoadBalancer.Common;
using LoadBalancer.Game;
using LoadBalancer.Jump;

namespace LoadBalancer.Server.Jump.Game
{
    [MethodsEnum(typeof(JumpMethods))]
    [ErrorsEnum(typeof(JumpErrors))]
    class JumpForGameServiceClient : ServiceClientBase, IJumpForGameServiceClient
    {
        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<JumpForGameServiceClient>();

        public void OnGameServiceAdded(AddGameServiceParameters parameters) =>
            Connect.RaiseEvent(JumpMethods.OnGameServiceAdded, parameters);

        public void OnGameServiceUpdated(UpdateGameServiceParameters parameters) =>
            Connect.RaiseEvent(JumpMethods.OnGameServiceUpdated, parameters);

        public PublishRoomParameters CreatePublishRoomParameters(BaseRoom room, IEnumerable<BasePlayer> players) => new PublishRoomParameters
        {
            RoomId = room.RoomId,
            RoomProperties = room.Properties.Extract<BaseRoomProperties>(),
            RoomPlayers = players.Select(player => new PublishRoomParameters.Player
            {
                PlayerId = player.PlayerId,
                PlayerProperties = player.Properties.Extract<BasePlayerProperties>(),
            }).ToArray(),
        };

        public void OnRoomPublished(PublishRoomParameters parameters)
        {
            Logger.LogDebug($"Jump Room Published {parameters.RoomId}");
            Connect.RaiseEvent(JumpMethods.OnRoomPublished, parameters);
        }

        public async Task<Result> OnRoomCreated(CreateRoomParameters parameters)
        {
            parameters = new CreateRoomParameters
            {
                RoomId = parameters.RoomId,
                RoomProperties = parameters.RoomProperties.Extract<BaseRoomProperties>(),
                PlayerId = parameters.PlayerId,
                PlayerProperties = parameters.PlayerProperties.Extract<BasePlayerProperties>(),
            };

            var result = await Connect.CallMethod<Result>(JumpMethods.OnRoomCreated, parameters);
            Logger.LogDebug($"Jump Room Created {parameters.RoomId} -> {result.Status}");
            return result;
        }

        public async Task<Result> OnRoomJoined(JoinRoomParameters parameters)
        {
            parameters = new JoinRoomParameters
            {
                RoomId = parameters.RoomId,
                PlayerId = parameters.PlayerId,
                PlayerProperties = parameters.PlayerProperties.Extract<BasePlayerProperties>(),
            };

            var result = await Connect.CallMethod<Result>(JumpMethods.OnRoomJoined, parameters);
            Logger.LogDebug($"Jump Room Joined {parameters.RoomId} -> {result.Status}");
            return result;
        }

        public async Task<Result> OnRoomLeaved(LeaveRoomParameters parameters)
        {
            var result = await Connect.CallMethod<Result>(JumpMethods.OnRoomLeaved, parameters);
            Logger.LogDebug($"Jump Room Leave {parameters.RoomId} -> {result.Status}");
            return result;
        }

        public async Task<Result> OnRoomUpdated(UpdateRoomParameters parameters)
        {
            parameters = new UpdateRoomParameters
            {
                RoomId = parameters.RoomId,
                RoomProperties = parameters.RoomProperties.Extract<BaseRoomProperties>(),
            };

            if (parameters.RoomProperties.Count == 0) return Result.Ok();

            var result = await Connect.CallMethod<Result>(JumpMethods.OnRoomUpdated, parameters);
            Logger.LogDebug($"Jump Room Updated {parameters.RoomId} -> {result.Status}");
            return result;
        }

        public async Task<Result> OnPlayerUpdated(UpdatePlayerParameters parameters)
        {
            parameters = new UpdatePlayerParameters
            {
                RoomId = parameters.RoomId,
                PlayerId = parameters.PlayerId,
                PlayerProperties = parameters.PlayerProperties.Extract<BasePlayerProperties>(),
            };

            if (parameters.PlayerProperties.Count == 0) return Result.Ok();

            var result = await Connect.CallMethod<Result>(JumpMethods.OnPlayerUpdated, parameters);
            Logger.LogDebug($"Jump Player Updated {parameters.RoomId} -> {result.Status}");
            return result;
        }
    }
}
