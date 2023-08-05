using LoadBalancer.Auth;
using LoadBalancer.Common;
using LoadBalancer.Game;
using LoadBalancer.Server.Common;

namespace LoadBalancer.Server.Game
{
    [MethodsEnum(typeof(GameMethods))]
    [ErrorsEnum(typeof(GameErrors))]
    public partial class GameServiceHandler : HandlerBase
    {
        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<GameServiceHandler>();

        public new GameService Service => (GameService)base.Service;

        public AuthContext AuthContext { get; private set; }
        public string UserId => AuthContext.Claims.GetValue<string>(AuthParameters.UserId);

        public ServerRoom Room { get; private set; }
        public ServerPlayer Player { get; private set; }

        protected CallResult Authenticate(CallContext call, AuthenticateParameters parameters)
        {
            switch (parameters.Provider)
            {
                case "Token":
                    if (string.IsNullOrEmpty(parameters.Token))
                        return call.Fail(GameErrors.Error_ParameterMissed, "Token");

                    if (!AuthTokenUtils.ValidateToken(parameters.Token, out var ctx))
                        return call.Fail(GameErrors.Error_TokenInvalid);

                    AuthContext = ctx;
                    break;

                default:
                    return call.Fail(GameErrors.Error_ProviderNotSupported, parameters.Provider);
            }

            IsAuthenticated = true;
            Logger.LogDebug($"Session {AuthContext.SessionId} authorized {UserId}");

            return call.Complete();
        }

        protected CallResult CreateRoom(CallContext call, CreateRoomParameters parameters)
        {
            if (!IsAuthenticated)
                return call.Fail(GameErrors.Error_NotAuthenticated);

            var room = Service.Rooms[parameters.RoomId];
            if (room != null)
                return call.Fail(GameErrors.Error_RoomAlreadyCreated);

            if (parameters.RoomProperties == null)
                parameters.RoomProperties = new KeyValueCollection();
            if (parameters.PlayerProperties == null)
                parameters.PlayerProperties = new KeyValueCollection();
            if (parameters.RoomObjects == null)
                parameters.RoomObjects = new CreateRoomParameters.Object[] { };

            room = new ServerRoom { RoomId = parameters.RoomId };
            room.Players.OnLeave += Players_OnLeave;

            room.UpdateProperties(parameters.RoomProperties, raise: false, playersToNotify: room.Players.None);

            foreach (var o in parameters.RoomObjects)
            {
                var obj = new ServerRoomObject { ObjectId = o.ObjectId, Tag = o.Tag };
                obj.UpdateProperties(o.ObjectProperties, raise: false, playersToNotify: room.Players.None);
                room.AddObject(obj, raise: false, playersToNotify: room.Players.None);
            }

            var player = new ServerPlayer { PlayerId = parameters.PlayerId, Handler = this };
            player.UpdateProperties(parameters.PlayerProperties, raise: false, playersToNotify: room.Players.None);
            player.Properties.IsHost = true;

            room.AddPlayer(player, raise: false, playersToNotify: room.Players.None);

            Room = room;
            Player = player;

            Service.Rooms.Add(room, raise: true);
            room.Start();
            Logger.LogDebug($"Room Created {room.RoomId} by {player.PlayerId} ip: {ConnectionInfo.RemoteIP}");

            void Players_OnLeave(ServerPlayer player)
            {
                if (player.Room.IsEmpty)
                {
                    Service.Rooms.Remove(player.Room, raise: true);
                    room.Stop();
                    Logger.LogInformation($"Room Removed {room.RoomId}");
                }
            }

            parameters.RoomProperties = room.Properties;
            parameters.PlayerProperties = player.Properties;

            Service.JumpServiceThread.EnqueueNew_IfConnected(async () =>
            {
                if (!(await Service.JumpService.OnRoomCreated(parameters)).IsOk)
                    Service.RepublishRoom(room);
            });

            return call.Complete(new CreateRoomResult
            {
                RoomId = room.RoomId,
                RoomProperties = room.Properties,
                PlayerId = player.PlayerId,
                PlayerProperties = player.Properties,
                RoomObjects = room.Objects.Select(p => new CreateRoomResult.Object
                {
                    ObjectId = p.ObjectId,
                    Tag = p.Tag,
                    ObjectProperties = p.Properties,
                }).ToArray(),
            });
        }

        protected CallResult JoinRoom(CallContext call, JoinRoomParameters parameters)
        {
            if (!IsAuthenticated)
                return call.Fail(GameErrors.Error_NotAuthenticated);

            var room = Service.Rooms[parameters.RoomId];
            if (room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            var player = room.Players[parameters.PlayerId];
            if (player != null)
                return call.Fail(GameErrors.Error_PlayerAlreadyJoined);

            if (room.Players.Count >= room.Properties.GetValue<int>(RoomKeys.MaxPlayers))
                return call.Fail(GameErrors.Error_RoomFull);

            if (parameters.PlayerProperties == null)
                parameters.PlayerProperties = new KeyValueCollection();

            player = new ServerPlayer { PlayerId = parameters.PlayerId, Handler = this };
            player.UpdateProperties(parameters.PlayerProperties, raise: false, playersToNotify: room.Players.None);

            Room = room;
            Player = player;
            return room.Thread.Enqueue(call, () =>
            {
                room.AddPlayer(player, raise: true, playersToNotify: room.Players.Except(player.PlayerId));
                Logger.LogInformation($"Room Joined {room.RoomId} by {player.PlayerId} ip: {ConnectionInfo.RemoteIP}");

                parameters.PlayerProperties = player.Properties;

                Service.JumpServiceThread.EnqueueNew_IfConnected(async () =>
                {
                    if (!(await Service.JumpService.OnRoomJoined(parameters)).IsOk)
                        Service.RepublishRoom(room);
                });

                return call.Complete(new JoinRoomResult
                {
                    RoomId = room.RoomId,
                    RoomProperties = room.Properties,
                    RoomPlayers = room.Players.Except(player.PlayerId).Select(p => new JoinRoomResult.Player
                    {
                        PlayerId = p.PlayerId,
                        PlayerProperties = p.Properties,
                    }).ToArray(),
                    RoomObjects = room.Objects.Select(p => new JoinRoomResult.Object
                    {
                        ObjectId = p.ObjectId,
                        Tag = p.Tag,
                        ObjectProperties = p.Properties,
                    }).ToArray(),
                    PlayerId = player.PlayerId,
                    PlayerProperties = player.Properties,
                });
            });
        }

        protected CallResult UpdateRoom(CallContext call, UpdateRoomParameters parameters)
        {
            if (!IsAuthenticated)
                return call.Fail(GameErrors.Error_NotAuthenticated);

            if (Room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Room.RoomId != parameters.RoomId)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (parameters.RoomProperties == null)
                parameters.RoomProperties = new KeyValueCollection();

            var room = Room;
            var player = Player;
            return room.Thread.Enqueue(call, () =>
            {
                room.UpdateProperties(parameters.RoomProperties, raise: true, playersToNotify: room.Players.Except(player.PlayerId));
                Logger.LogDebug($"Room Updated {room.RoomId} by {player.PlayerId}: ({parameters.RoomProperties.Count} props)");

                Service.JumpServiceThread.EnqueueNew_IfConnected(async () =>
                {
                    if (!(await Service.JumpService.OnRoomUpdated(parameters)).IsOk)
                        Service.RepublishRoom(room);
                });

                return call.Complete();
            });
        }

        protected CallResult UpdatePlayer(CallContext call, UpdatePlayerParameters parameters)
        {
            if (!IsAuthenticated)
                return call.Fail(GameErrors.Error_NotAuthenticated);

            if (Room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Room.RoomId != parameters.RoomId)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Player.PlayerId != parameters.PlayerId)
                return call.Fail(GameErrors.Error_PlayerNotFound);

            if (parameters.PlayerProperties == null)
                parameters.PlayerProperties = new KeyValueCollection();

            var room = Room;
            var player = Player;
            return room.Thread.Enqueue(call, () =>
            {
                player.UpdateProperties(parameters.PlayerProperties, raise: true, playersToNotify: room.Players.Except(player.PlayerId));
                Logger.LogDebug($"Player Updated in room {room.RoomId} by {player.PlayerId}: ({parameters.PlayerProperties.Count} props)");

                Service.JumpServiceThread.EnqueueNew_IfConnected(async () =>
                {
                    if (!(await Service.JumpService.OnPlayerUpdated(parameters)).IsOk)
                        Service.RepublishRoom(room);
                });

                return call.Complete();
            });
        }

        protected CallResult RaiseRoomEvent(CallContext call, RoomEvent parameters)
        {
            if (!IsAuthenticated)
                return call.Fail(GameErrors.Error_NotAuthenticated);

            if (Room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Room.RoomId != parameters.RoomId)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (parameters.Parameters == null)
                parameters.Parameters = new KeyValueCollection();

            parameters.SenderId = Player.PlayerId;

            var room = Room;
            var player = Player;
            return room.Thread.Enqueue(call, () =>
            {
                room.RaiseEventReceived(parameters);

                player.RaiseRoomEvent(parameters.Name, parameters.Parameters, playersToNotify: room.Players.Except(player.PlayerId).Only(parameters.PlayerId));

                Logger.LogDebug($"Event Raised in room {room.RoomId} by {player.PlayerId}: {call.Name}");

                return call.Complete();
            });
        }

        protected CallResult SpawnObject(CallContext call, SpawnObjectParameters parameters)
        {
            if (!IsAuthenticated)
                return call.Fail(GameErrors.Error_NotAuthenticated);

            if (Room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Room.RoomId != parameters.RoomId)
                return call.Fail(GameErrors.Error_RoomNotFound);

            var obj = Room.Objects[parameters.ObjectId];
            if (obj != null)
                return call.Fail(GameErrors.Error_ObjectAlreadySpawned);

            if (parameters.ObjectProperties == null)
                parameters.ObjectProperties = new KeyValueCollection();

            var room = Room;
            var player = Player;
            obj = new ServerRoomObject { ObjectId = parameters.ObjectId, Tag = parameters.Tag };
            obj.UpdateProperties(parameters.ObjectProperties, raise: false, playersToNotify: room.Players.None);

            return room.Thread.Enqueue(call, () =>
            {
                room.AddObject(obj, raise: true, playersToNotify: room.Players.Except(player.PlayerId));
                Logger.LogInformation($"Object Spawned in room {room.RoomId} by {player.PlayerId}: {parameters.Tag} {parameters.ObjectId} ({parameters.ObjectProperties.Count} props)");

                return call.Complete(new SpawnObjectResult
                {
                    RoomId = room.RoomId,
                    ObjectId = obj.ObjectId,
                    Tag = obj.Tag,
                    ObjectProperties = obj.Properties,
                });
            });
        }

        protected CallResult UpdateObject(CallContext call, UpdateObjectParameters parameters)
        {
            if (!IsAuthenticated)
                return call.Fail(GameErrors.Error_NotAuthenticated);

            if (Room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Room.RoomId != parameters.RoomId)
                return call.Fail(GameErrors.Error_RoomNotFound);

            var obj = Room.Objects[parameters.ObjectId];
            if (obj == null)
                return call.Fail(GameErrors.Error_ObjectNotFound);

            if (parameters.ObjectProperties == null)
                parameters.ObjectProperties = new KeyValueCollection();

            var room = Room;
            var player = Player;

            return room.Thread.Enqueue(call, () =>
            {
                obj.UpdateProperties(parameters.ObjectProperties, raise: true, playersToNotify: room.Players.Except(player.PlayerId));
                Logger.LogDebug($"Object Updated in room {room.RoomId} by {player.PlayerId}: {parameters.ObjectId} ({parameters.ObjectProperties.Count} props)");

                return call.Complete();
            });
        }

        protected CallResult DestroyObject(CallContext call, DestroyObjectParameters parameters)
        {
            if (!IsAuthenticated)
                return call.Fail(GameErrors.Error_NotAuthenticated);

            if (Room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Room.RoomId != parameters.RoomId)
                return call.Fail(GameErrors.Error_RoomNotFound);

            var obj = Room.Objects[parameters.ObjectId];
            if (obj == null)
                return call.Fail(GameErrors.Error_ObjectNotFound);

            var room = Room;
            var player = Player;

            return room.Thread.Enqueue(call, () =>
            {
                obj.DestroyObject(raise: true, playersToNotify: room.Players.Except(player.PlayerId));
                Logger.LogDebug($"Object Destroyed in room {room.RoomId} by {player.PlayerId}: {parameters.ObjectId}");

                return call.Complete();
            });
        }

        protected override void OnDisconnected(CallContext call)
        {
            if (Room != null)
            {
                var room = Room;
                var player = Player;
                room.Thread.Enqueue(call, () =>
                {
                    room.RemovePlayer(player, raise: true, room.Players.Except(player.PlayerId));
                    Logger.LogInformation($"Room Leave {room.RoomId} by {player.PlayerId} ip: {ConnectionInfo.RemoteIP}");

                    var parameters = new LeaveRoomParameters
                    {
                        RoomId = room.RoomId,
                        PlayerId = player.PlayerId,
                    };

                    Service.JumpServiceThread.EnqueueNew_IfConnected(async () =>
                    {
                        if (!(await Service.JumpService.OnRoomLeaved(parameters)).IsOk)
                            Service.RepublishRoom(room);
                    });

                    player.Room = null;
                    player.Handler = null;

                    Room = null;
                    Player = null;
                });
            }
        }

#if DEBUG
        protected CallResult OnJumpServiceConnectEnabled(CallContext call)
        {
            Service.EnableJumpServiceConnect();
            return call.Complete();
        }

        protected CallResult OnJumpServiceConnectDisabled(CallContext call)
        {
            Service.DisableJumpServiceConnect();
            return call.Complete();
        }
#endif

        public void OnRoomUpdated(UpdateRoomParameters evt) => RaiseEvent(GameMethods.OnRoomUpdated, evt);

        public void OnPlayerUpdated(UpdatePlayerParameters evt) => RaiseEvent(GameMethods.OnPlayerUpdated, evt);

        public void OnRoomJoined(JoinRoomParameters evt) => RaiseEvent(GameMethods.OnRoomJoined, evt);

        public void OnRoomLeaved(LeaveRoomParameters evt) => RaiseEvent(GameMethods.OnRoomLeaved, evt);

        public void OnRoomEventRaised(RoomEvent evt) => RaiseEvent(GameMethods.OnRoomEventRaised, evt);

        public void OnObjectSpawned(SpawnObjectParameters evt) => RaiseEvent(GameMethods.OnObjectSpawned, evt);

        public void OnObjectUpdated(UpdateObjectParameters evt) => RaiseEvent(GameMethods.OnObjectUpdated, evt);

        public void OnObjectDestroyed(DestroyObjectParameters evt) => RaiseEvent(GameMethods.OnObjectDestroyed, evt);
    }
}
