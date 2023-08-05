using LoadBalancer.Auth;
using LoadBalancer.Client.Common;
using LoadBalancer.Common;
using System.Threading.Tasks;

namespace LoadBalancer.Client.Auth
{
    [MethodsEnum(typeof(AuthMethods))]
    [ErrorsEnum(typeof(AuthErrors))]
    class AuthServiceClient : ServiceClientBase, IAuthServiceClient
    {
        public Task<AuthenticateResult> Authenticate(AuthenticateParameters parameters) =>
            Connect.CallMethod<AuthenticateResult>(AuthMethods.Authenticate, parameters);

        public Task<ListJumpServicesResult> ListJumpServices(ListJumpServicesParameters parameters) =>
            Connect.CallMethod<ListJumpServicesResult>(AuthMethods.ListJumpServices, parameters,
                heavyParameters: true, heavyResult: true);

        public Task<SelectClosestServiceResult> SelectClosestService(Endpoint[] endpoints) =>
            ServiceConnect.SelectClosestService(endpoints);
    }
}
