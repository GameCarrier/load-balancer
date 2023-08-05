using LoadBalancer.Auth;
using LoadBalancer.Common;
using LoadBalancer.Jump;
using LoadBalancer.Server.Common;

namespace LoadBalancer.Server.Jump
{
    [MethodsEnum(typeof(JumpMethods))]
    [ErrorsEnum(typeof(JumpErrors))]
    public partial class JumpServiceHandler : HandlerBase
    {
        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<JumpServiceHandler>();

        public new JumpService Service => (JumpService)base.Service;

        public AuthContext AuthContext { get; private set; }
        public string UserId => AuthContext.Claims.GetValue<string>(AuthParameters.UserId);

        protected CallResult Authenticate(CallContext call, AuthenticateParameters parameters)
        {
            switch (parameters.Provider)
            {
                case "Token":
                    if (string.IsNullOrEmpty(parameters.Token))
                        return call.Fail(JumpErrors.Error_ParameterMissed, "Token");

                    if (!AuthTokenUtils.ValidateToken(parameters.Token, out var ctx))
                        return call.Fail(JumpErrors.Error_TokenInvalid);

                    AuthContext = ctx;
                    break;

                default:
                    return call.Fail(JumpErrors.Error_ProviderNotSupported, parameters.Provider);
            }

            IsAuthenticated = true;
            Logger.LogDebug($"Session {AuthContext.SessionId} authorized {UserId}");

            return call.Complete();
        }

        protected CallResult FindRoom(CallContext call, FindRoomParameters parameters)
        {
            if (!IsAuthenticated)
                return call.Fail(JumpErrors.Error_NotAuthenticated);

            if (!string.IsNullOrEmpty(parameters.TitleId) && parameters.TitleId != Service.Settings.TitleId)
                return call.Fail(JumpErrors.Error_WrongTitle);

            if (!string.IsNullOrEmpty(parameters.Version) && parameters.Version != Service.Settings.Version)
                return call.Fail(JumpErrors.Error_WrongVersion);

            var matched = AvailableGameServices.SelectMany(state => state.Rooms)
                .Where(room =>
                {
                    if (!string.IsNullOrEmpty(parameters.RoomId) && room.RoomId != parameters.RoomId)
                        return false;

                    if (parameters.RoomProperties != null && !room.Properties.Match(parameters.RoomProperties))
                        return false;

                    if (parameters.PlayerIds != null && !parameters.PlayerIds.Intersect(room.Players.Select(p => p.PlayerId)).Any())
                        return false;

                    if (room.Players.Count >= room.Properties.MaxPlayers)
                        return false;

                    return true;
                }).ToList();

            if (!matched.Any())
                return call.Fail(JumpErrors.Error_RoomNotFound);

            var locatedRooms = matched.Select(room => new RoomLocator
            {
                ServiceEndpoint = room.ServiceEndpoint,
                RoomId = room.RoomId,
                RoomProperties = room.Properties,
                IsExistingRoom = true,
            }).ToArray();

            return call.Complete(new FindRoomResult { Rooms = locatedRooms });
        }

        protected CallResult FindServer(CallContext call, FindServerParameters parameters)
        {
            if (!IsAuthenticated)
                return call.Fail(JumpErrors.Error_NotAuthenticated);

            if (!string.IsNullOrEmpty(parameters.TitleId) && parameters.TitleId != Service.Settings.TitleId)
                return call.Fail(JumpErrors.Error_WrongTitle);

            if (!string.IsNullOrEmpty(parameters.Version) && parameters.Version != Service.Settings.Version)
                return call.Fail(JumpErrors.Error_WrongVersion);

            var gameService = AvailableGameServices.FirstOrDefault();
            if (gameService == null)
                return call.Fail(JumpErrors.Error_ServerFull);

            return call.Complete(new FindServerResult
            {
                Room = new RoomLocator
                {
                    ServiceEndpoint = gameService.ServiceEndpoint,
                    RoomId = Guid.NewGuid().ToString(),
                    RoomProperties = parameters.RoomProperties,
                    IsExistingRoom = false,
                }
            });
        }
    }
}
