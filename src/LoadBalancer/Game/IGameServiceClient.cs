using LoadBalancer.Auth;
using System.Threading.Tasks;

namespace LoadBalancer.Game
{
    public interface IGameServiceClient
    {
        IClientRoom Room { get; }
        IClientPlayer Player { get; }

        Task<AuthenticateResult> Authenticate(AuthenticateParameters parameters);
        Task<CreateRoomResult> CreateRoom(CreateRoomParameters parameters);
        Task<JoinRoomResult> JoinRoom(JoinRoomParameters parameters);
        Task<SpawnObjectResult> SpawnObject(SpawnObjectParameters parameters);

#if DEBUG
        void OnJumpServiceConnectEnabled();
        void OnJumpServiceConnectDisabled();
#endif
    }
}
