using LoadBalancer.Auth;
using LoadBalancer.Client.Common;
using LoadBalancer.Common;

namespace LoadBalancer.Server.Auth.Jump
{
    [MethodsEnum(typeof(AuthMethods))]
    [ErrorsEnum(typeof(AuthErrors))]
    class AuthForJumpServiceClient : ServiceClientBase, IAuthForJumpServiceClient
    {
        public void OnJumpServiceAdded(AddJumpServiceParameters parameters) =>
            Connect.RaiseEvent(AuthMethods.OnJumpServiceAdded, parameters);
    }
}
