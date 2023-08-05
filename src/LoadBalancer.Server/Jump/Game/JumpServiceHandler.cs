using LoadBalancer.Game;
using LoadBalancer.Server.Common;
using LoadBalancer.Server.Jump.Game;

namespace LoadBalancer.Server.Jump
{
    public partial class JumpServiceHandler
    {
        private GameServiceState GameServiceState;

        private RoomDescription FindRoom(string roomId) =>
            GameServices.SelectMany(state => state.Rooms).FirstOrDefault(r => r.RoomId == roomId);

        private List<GameServiceState> GameServices => Service.Handlers
            .FindHandlers<JumpServiceHandler>(h => h.GameServiceState != null)
            .Select(h => h.GameServiceState)
            .ToList();

        private List<GameServiceState> AvailableGameServices => GameServices
            .Where(state => state.ServiceProperties.LoadLevel <= SystemLoadLevel.High)
            .OrderBy(state => state.ServiceProperties.LoadLevel)
            .ToList();

        protected CallResult OnGameServiceAdded(CallContext call, AddGameServiceParameters parameters)
        {
            Logger.LogInformation($"GameService registered on Jump {parameters.ServiceEndpoint}");

            GameServiceState = new GameServiceState
            {
                Handler = this,
                ServiceEndpoint = parameters.ServiceEndpoint,
            };
            GameServiceState.ServiceProperties.Merge(parameters.ServiceProperties);

            var obsolette = GameServices
                .Where(state => GameServiceState.ServiceEndpoint.Equals(state.ServiceEndpoint))
                .Where(state => !ReferenceEquals(GameServiceState, state))
                .ToList();

            foreach (var state in obsolette)
            {
                Logger.LogDebug($"Force Disconnect GameService S2S handler on OnGameServiceAdded {state.ServiceEndpoint}");
                state.Handler.Disconnect();
            }

            return call.Complete();
        }

        protected CallResult OnGameServiceUpdated(CallContext call, UpdateGameServiceParameters parameters)
        {
            Logger.LogInformation($"GameService updated on Jump {GameServiceState.ServiceEndpoint}");
            GameServiceState.ServiceProperties.Merge(parameters.ServiceProperties);
            return call.Complete();
        }

        protected CallResult OnRoomPublished(CallContext call, PublishRoomParameters parameters) =>
            Service.Thread.Enqueue(call, () =>
            {
                bool newRoom = false;
                var room = FindRoom(parameters.RoomId);
                if (room != null)
                {
                    room.Properties.Clear();
                    room.Players.Clear(raise: false);
                }
                else
                {
                    newRoom = true;
                    room = new RoomDescription { RoomId = parameters.RoomId, ServiceEndpoint = GameServiceState.ServiceEndpoint };
                }

                room.UpdateProperties(parameters.RoomProperties, raise: false);

                foreach (var p in parameters.RoomPlayers)
                {
                    var player = new PlayerDescription { PlayerId = p.PlayerId };
                    player.UpdateProperties(p.PlayerProperties, raise: false);
                    room.Players.Add(player, raise: false);
                }

                if (newRoom && room.Players.Count > 0)
                {
                    GameServiceState.Rooms.Add(room, raise: true);
                    Logger.LogDebug($"Jump Room Added {room.RoomId} on Publish");
                }

                if (!newRoom && room.Players.Count == 0)
                {
                    GameServiceState.Rooms.Remove(room, raise: true);
                    Logger.LogDebug($"Jump Room Removed {room.RoomId} on Publish");
                }

                return call.Complete();
            });

        protected CallResult OnRoomCreated(CallContext call, CreateRoomParameters parameters) =>
            Service.Thread.Enqueue(call, () =>
            {
                var room = FindRoom(parameters.RoomId);
                if (room != null)
                    return call.Fail(GameErrors.Error_RoomAlreadyCreated);

                room = new RoomDescription { RoomId = parameters.RoomId, ServiceEndpoint = GameServiceState.ServiceEndpoint };

                room.UpdateProperties(parameters.RoomProperties, raise: false);

                var player = new PlayerDescription { PlayerId = parameters.PlayerId };
                player.UpdateProperties(parameters.PlayerProperties, raise: false);

                room.Players.Add(player, raise: false);

                GameServiceState.Rooms.Add(room, raise: true);
                Logger.LogDebug($"Jump Room Added {room.RoomId}");

                return call.Complete();
            });

        protected CallResult OnRoomJoined(CallContext call, JoinRoomParameters parameters) =>
            Service.Thread.Enqueue(call, () =>
            {
                var room = FindRoom(parameters.RoomId);
                if (room == null)
                    return call.Fail(GameErrors.Error_RoomNotFound);

                var player = room.Players[parameters.PlayerId];
                if (player != null)
                    return call.Fail(GameErrors.Error_PlayerAlreadyJoined);

                player = new PlayerDescription { PlayerId = parameters.PlayerId };
                player.UpdateProperties(parameters.PlayerProperties, raise: false);

                room.Players.Add(player, raise: true);
                Logger.LogDebug($"Jump Room Joined {room.RoomId}");

                return call.Complete();
            });

        protected CallResult OnRoomLeaved(CallContext call, LeaveRoomParameters parameters) =>
            Service.Thread.Enqueue(call, () =>
            {
                var room = FindRoom(parameters.RoomId);
                if (room == null)
                    return call.Fail(GameErrors.Error_RoomNotFound);

                var player = room.Players[parameters.PlayerId];
                if (player == null)
                    return call.Fail(GameErrors.Error_PlayerNotFound);

                room.Players.Remove(player, raise: true);
                Logger.LogDebug($"Jump Room Leave {room.RoomId}");

                if (room.Players.Count == 0)
                {
                    GameServiceState.Rooms.Remove(room, raise: true);
                    Logger.LogDebug($"Jump Room Removed {room.RoomId}");
                }

                return call.Complete();
            });

        protected CallResult OnRoomUpdated(CallContext call, UpdateRoomParameters parameters) =>
            Service.Thread.Enqueue(call, () =>
            {
                var room = FindRoom(parameters.RoomId);
                if (room == null)
                    return call.Fail(GameErrors.Error_RoomNotFound);

                room.UpdateProperties(parameters.RoomProperties, raise: true);
                Logger.LogDebug($"Jump Room Updated {parameters.RoomId}");

                return call.Complete();
            });

        protected CallResult OnPlayerUpdated(CallContext call, UpdatePlayerParameters parameters) =>
            Service.Thread.Enqueue(call, () =>
            {
                var room = FindRoom(parameters.RoomId);
                if (room == null)
                    return call.Fail(GameErrors.Error_RoomNotFound);

                var player = room.Players[parameters.PlayerId];
                if (player == null)
                    return call.Fail(GameErrors.Error_PlayerNotFound);

                player.UpdateProperties(parameters.PlayerProperties, raise: true);
                Logger.LogDebug($"Jump Player Updated {parameters.RoomId}");

                return call.Complete();
            });
    }
}
