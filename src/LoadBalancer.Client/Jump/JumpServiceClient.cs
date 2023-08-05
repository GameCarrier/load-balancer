using LoadBalancer.Auth;
using LoadBalancer.Client.Common;
using LoadBalancer.Common;
using LoadBalancer.Jump;
using System.Threading.Tasks;

namespace LoadBalancer.Client.Jump
{
    [MethodsEnum(typeof(JumpMethods))]
    [ErrorsEnum(typeof(JumpErrors))]
    class JumpServiceClient : ServiceClientBase, IJumpServiceClient
    {
        public Task<AuthenticateResult> Authenticate(AuthenticateParameters parameters) =>
            Connect.CallMethod<AuthenticateResult>(JumpMethods.Authenticate, parameters);

        public Task<FindRoomResult> FindRoom(FindRoomParameters parameters) =>
            Connect.CallMethod<FindRoomResult>(JumpMethods.FindRoom, parameters);

        public Task<FindServerResult> FindServer(FindServerParameters parameters) =>
            Connect.CallMethod<FindServerResult>(JumpMethods.FindServer, parameters);
    }
}
