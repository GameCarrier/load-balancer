using LoadBalancer.Auth;
using LoadBalancer.Client.Common;
using LoadBalancer.Common;
using LoadBalancer.Game;
using System.Threading.Tasks;

namespace LoadBalancer.Client.Game
{
    [MethodsEnum(typeof(GameMethods))]
    [ErrorsEnum(typeof(GameErrors))]
    class GameServiceClient : ServiceClientBase, IGameServiceClient
    {
        public ClientRoom Room { get; private set; }
        public ClientPlayer Player { get; private set; }

        IClientRoom IGameServiceClient.Room => Room;
        IClientPlayer IGameServiceClient.Player => Player;

        public Task<AuthenticateResult> Authenticate(AuthenticateParameters parameters) =>
            Connect.CallMethod<AuthenticateResult>(GameMethods.Authenticate, parameters);

        public async Task<CreateRoomResult> CreateRoom(CreateRoomParameters parameters)
        {
            if (Room != null)
                throw new ResultException(GameErrors.Error_RoomAlreadyCreated);

            var result = await Connect.CallMethod<CreateRoomResult>(GameMethods.CreateRoom, parameters);
            if (result.IsOk)
            {
                var room = new ClientRoom { RoomId = result.RoomId, Service = this };
                room.UpdateProperties(result.RoomProperties, raise: false, notify: false);

                foreach (var o in result.RoomObjects)
                {
                    var other = new ClientRoomObject { ObjectId = o.ObjectId, Tag = o.Tag };
                    other.UpdateProperties(o.ObjectProperties, raise: false, notify: false);
                    room.Objects.Add(other, raise: false);
                }

                var player = new ClientPlayer { PlayerId = result.PlayerId, IsMyPlayer = true };
                player.UpdateProperties(result.PlayerProperties, raise: false, notify: false);

                room.Players.Add(player, raise: false);

                Room = room;
                Player = player;
            }

            return result;
        }

        public async Task<JoinRoomResult> JoinRoom(JoinRoomParameters parameters)
        {
            if (Room != null)
                throw new ResultException(GameErrors.Error_RoomAlreadyCreated);

            var result = await Connect.CallMethod<JoinRoomResult>(GameMethods.JoinRoom, parameters);
            if (result.IsOk)
            {
                var room = new ClientRoom { RoomId = result.RoomId, Service = this };
                room.UpdateProperties(result.RoomProperties, raise: false, notify: false);

                foreach (var o in result.RoomObjects)
                {
                    var other = new ClientRoomObject { ObjectId = o.ObjectId, Tag = o.Tag };
                    other.UpdateProperties(o.ObjectProperties, raise: false, notify: false);
                    room.Objects.Add(other, raise: false);
                }

                foreach (var p in result.RoomPlayers)
                {
                    var other = new ClientPlayer { PlayerId = p.PlayerId };
                    other.UpdateProperties(p.PlayerProperties, raise: false, notify: false);
                    room.Players.Add(other, raise: false);
                }

                var player = new ClientPlayer { PlayerId = result.PlayerId, IsMyPlayer = true };
                player.UpdateProperties(result.PlayerProperties, raise: false, notify: false);
                room.Players.Add(player, raise: false);

                Room = room;
                Player = player;
            }

            return result;
        }

        public void UpdateRoom(UpdateRoomParameters parameters)
        {
            if (Room == null)
                throw new ResultException(GameErrors.Error_RoomNotFound);

            Connect.RaiseEvent(GameMethods.UpdateRoom, parameters);
        }

        public void UpdatePlayer(UpdatePlayerParameters parameters)
        {
            if (Room == null)
                throw new ResultException(GameErrors.Error_RoomNotFound);

            Connect.RaiseEvent(GameMethods.UpdatePlayer, parameters);
        }

        public void RaiseRoomEvent(RoomEvent parameters)
        {
            if (Room == null)
                throw new ResultException(GameErrors.Error_RoomNotFound);

            Connect.RaiseEvent(GameMethods.RaiseRoomEvent, parameters);
        }

        public async Task<SpawnObjectResult> SpawnObject(SpawnObjectParameters parameters)
        {
            if (Room == null)
                throw new ResultException(GameErrors.Error_RoomNotFound);

            parameters.RoomId = Room.RoomId;

            var result = await Connect.CallMethod<SpawnObjectResult>(GameMethods.SpawnObject, parameters);
            if (result.IsOk)
            {
                var obj = new ClientRoomObject { ObjectId = result.ObjectId, Tag = result.Tag };
                obj.UpdateProperties(result.ObjectProperties, raise: false, notify: false);
                Room.Objects.Add(obj, raise: false);
            }
            return result;
        }

        public void UpdateObject(UpdateObjectParameters parameters)
        {
            if (Room == null)
                throw new ResultException(GameErrors.Error_RoomNotFound);

            Connect.RaiseEvent(GameMethods.UpdateObject, parameters);
        }

        public void DestroyObject(DestroyObjectParameters parameters)
        {
            if (Room == null)
                throw new ResultException(GameErrors.Error_RoomNotFound);

            Connect.RaiseEvent(GameMethods.DestroyObject, parameters);
        }

#if DEBUG
        public void OnJumpServiceConnectEnabled() => Connect.RaiseEvent(GameMethods.OnJumpServiceConnectEnabled);
        public void OnJumpServiceConnectDisabled() => Connect.RaiseEvent(GameMethods.OnJumpServiceConnectDisabled);
#endif

        protected CallResult OnRoomUpdated(ClientCallContext call, UpdateRoomParameters parameters)
        {
            if (Room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Room.RoomId != parameters.RoomId)
                return call.Fail(GameErrors.Error_RoomNotFound);

            Room.UpdateProperties(parameters.RoomProperties, raise: true, notify: false);

            return call.Complete();
        }

        protected CallResult OnPlayerUpdated(ClientCallContext call, UpdatePlayerParameters parameters)
        {
            if (Room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Room.RoomId != parameters.RoomId)
                return call.Fail(GameErrors.Error_RoomNotFound);

            var player = Room.Players[parameters.PlayerId];
            if (player == null)
                return call.Fail(GameErrors.Error_PlayerNotFound);

            player.UpdateProperties(parameters.PlayerProperties, raise: true, notify: false);

            return call.Complete();
        }

        protected CallResult OnRoomJoined(ClientCallContext call, JoinRoomParameters parameters)
        {
            if (Room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Room.RoomId != parameters.RoomId)
                return call.Fail(GameErrors.Error_RoomNotFound);

            var player = Room.Players[parameters.PlayerId];
            if (player != null)
                return call.Fail(GameErrors.Error_PlayerAlreadyJoined);

            player = new ClientPlayer { PlayerId = parameters.PlayerId };
            player.UpdateProperties(parameters.PlayerProperties, raise: false, notify: false);

            Room.Players.Add(player, raise: true);

            return call.Complete();
        }

        protected CallResult OnRoomLeaved(ClientCallContext call, LeaveRoomParameters parameters)
        {
            if (Room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Room.RoomId != parameters.RoomId)
                return call.Fail(GameErrors.Error_RoomNotFound);

            var player = Room.Players[parameters.PlayerId];
            if (player == null)
                return call.Fail(GameErrors.Error_PlayerNotFound);
            
            Room.Players.Remove(player, raise: true);
            
            player.RaiseOnLeave();

            return call.Complete();
        }

        protected CallResult OnRoomEventRaised(ClientCallContext call, RoomEvent parameters)
        {
            if (Room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Room.RoomId != parameters.RoomId)
                return call.Fail(GameErrors.Error_RoomNotFound);

            Room.RaiseEventReceived(parameters);

            return call.Complete();
        }

        protected CallResult OnObjectSpawned(ClientCallContext call, SpawnObjectParameters parameters)
        {
            if (Room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Room.RoomId != parameters.RoomId)
                return call.Fail(GameErrors.Error_RoomNotFound);

            var obj = Room.Objects[parameters.ObjectId];
            if (obj != null)
                return call.Fail(GameErrors.Error_ObjectAlreadySpawned);

            obj = new ClientRoomObject { ObjectId = parameters.ObjectId, Tag = parameters.Tag };
            obj.UpdateProperties(parameters.ObjectProperties, raise: false, notify: false);

            Room.Objects.Add(obj, raise: true);

            return call.Complete();
        }

        protected CallResult OnObjectUpdated(ClientCallContext call, UpdateObjectParameters parameters)
        {
            if (Room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Room.RoomId != parameters.RoomId)
                return call.Fail(GameErrors.Error_RoomNotFound);

            var obj = Room.Objects[parameters.ObjectId];
            if (obj == null)
                return call.Fail(GameErrors.Error_ObjectNotFound);

            obj.UpdateProperties(parameters.ObjectProperties, raise: true, notify: false);

            return call.Complete();
        }

        protected CallResult OnObjectDestroyed(ClientCallContext call, DestroyObjectParameters parameters)
        {
            if (Room == null)
                return call.Fail(GameErrors.Error_RoomNotFound);

            if (Room.RoomId != parameters.RoomId)
                return call.Fail(GameErrors.Error_RoomNotFound);

            var obj = Room.Objects[parameters.ObjectId];
            if (obj == null)
                return call.Fail(GameErrors.Error_ObjectNotFound);

            obj.Destroy(raise: true, notify: false);

            obj.RaiseOnDestroy();

            return call.Complete();
        }

        protected override void OnDisconnected(ClientCallContext call)
        {
            Room.Service = null;
            Player.Room = null;

            Room = null;
            Player = null;
        }
    }
}
