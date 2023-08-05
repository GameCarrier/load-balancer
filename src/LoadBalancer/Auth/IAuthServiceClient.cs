using LoadBalancer.Common;
using System.Threading.Tasks;

namespace LoadBalancer.Auth
{
    public interface IAuthServiceClient
    {
        Task<AuthenticateResult> Authenticate(AuthenticateParameters parameters);
        Task<ListJumpServicesResult> ListJumpServices(ListJumpServicesParameters parameters);
        Task<SelectClosestServiceResult> SelectClosestService(Endpoint[] endpoints);
    }
}
