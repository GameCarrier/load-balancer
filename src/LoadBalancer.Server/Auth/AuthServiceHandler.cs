using LoadBalancer.Auth;
using LoadBalancer.Common;
using LoadBalancer.Server.Common;

namespace LoadBalancer.Server.Auth
{
    [MethodsEnum(typeof(AuthMethods))]
    [ErrorsEnum(typeof(AuthErrors))]
    public partial class AuthServiceHandler : HandlerBase
    {
        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<AuthServiceHandler>();

        public new AuthService Service => (AuthService)base.Service;

        protected CallResult Authenticate(CallContext call, AuthenticateParameters parameters)
        {
            Logger.LogDebug($"Authenticate on {parameters.Provider}");

            var claims = new KeyValueCollection();
            switch (parameters.Provider)
            {
                case "Test":
                    string userName = parameters.Params.GetValue<string>(AuthParameters.UserName);
                    if (string.IsNullOrEmpty(userName))
                        return call.Fail(AuthErrors.Error_ParameterMissed, "UserName");

                    claims.SetValue(AuthParameters.UserId, userName);
                    claims.SetValue(AuthParameters.UserName, userName);
                    claims.SetValue(AuthParameters.LanguageId, 1);
                    break;

                default:
                    return call.Fail(AuthErrors.Error_ProviderNotSupported, parameters.Provider);
            }

            IsAuthenticated = true;

            string sessionId = Guid.NewGuid().ToString();
            string token = AuthTokenUtils.GenerateToken(sessionId, claims);
            return call.Complete(new AuthenticateResult { AuthToken = token });
        }

        protected CallResult ListJumpServices(CallContext call, ListJumpServicesParameters parameters)
        {
            if (!IsAuthenticated)
                return call.Fail(AuthErrors.Error_NotAuthenticated);

            var endpoints = JumpSerivces.Where(state =>
            {
                if (!string.IsNullOrEmpty(parameters.Region) && state.ServiceProperties.Region != parameters.Region)
                    return false;

                if (!string.IsNullOrEmpty(parameters.TitleId) && state.ServiceProperties.Region != parameters.TitleId)
                    return false;

                if (!string.IsNullOrEmpty(parameters.Version) && state.ServiceProperties.Region != parameters.Version)
                    return false;

                return true;
            }).Select(state => state.ServiceEndpoint).ToArray();

            if (!endpoints.Any())
                return call.Fail(AuthErrors.Error_JumpServerNotFound);

            return call.Complete(new ListJumpServicesResult { Endpoints = endpoints });
        }
    }
}
