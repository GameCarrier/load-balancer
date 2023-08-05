using LoadBalancer.Auth;
using System.Threading.Tasks;

namespace LoadBalancer.Jump
{
    public interface IJumpServiceClient
    {
        Task<AuthenticateResult> Authenticate(AuthenticateParameters parameters);
        Task<FindRoomResult> FindRoom(FindRoomParameters parameters);
        Task<FindServerResult> FindServer(FindServerParameters parameters);
    }
}
