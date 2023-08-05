using LoadBalancer.Common;
using LoadBalancer.Game;

namespace LoadBalancer.Server.Jump.Game
{
    public interface IJumpForGameServiceClient
    {
        void OnGameServiceAdded(AddGameServiceParameters parameters);
        void OnGameServiceUpdated(UpdateGameServiceParameters parameters);
        PublishRoomParameters CreatePublishRoomParameters(BaseRoom room, IEnumerable<BasePlayer> players);
        void OnRoomPublished(PublishRoomParameters parameters);
        Task<Result> OnRoomCreated(CreateRoomParameters parameters);
        Task<Result> OnRoomJoined(JoinRoomParameters parameters);
        Task<Result> OnRoomLeaved(LeaveRoomParameters parameters);
        Task<Result> OnRoomUpdated(UpdateRoomParameters parameters);
        Task<Result> OnPlayerUpdated(UpdatePlayerParameters parameters);
    }
}
