using LoadBalancer.Server.Auth.Jump;
using LoadBalancer.Server.Common;

namespace LoadBalancer.Server.Auth
{
    public partial class AuthServiceHandler
    {
        private List<JumpServiceState> JumpSerivces => Service.Handlers
            .FindHandlers<AuthServiceHandler>(h => h.JumpServiceState != null)
            .Select(h => h.JumpServiceState)
            .ToList();

        private JumpServiceState JumpServiceState { get; set; }

        protected CallResult OnJumpServiceAdded(CallContext call, AddJumpServiceParameters parameters)
        {
            Logger.LogInformation($"JumpService registered on Auth {parameters.ServiceEndpoint}");

            JumpServiceState = new JumpServiceState
            {
                Handler = this,
                ServiceEndpoint = parameters.ServiceEndpoint,
            };
            JumpServiceState.ServiceProperties.Merge(parameters.ServiceProperties);

            var obsolette = JumpSerivces
                .Where(state => JumpServiceState.ServiceEndpoint.Equals(state.ServiceEndpoint))
                .Where(state => !ReferenceEquals(JumpServiceState, state))
                .ToList();

            foreach (var state in obsolette)
            {
                Logger.LogInformation($"Force Disconnect JumpService S2S handler on OnJumpServiceAdded {state.ServiceEndpoint}");
                state.Handler.Disconnect();
            }

            return call.Complete();
        }
    }
}
