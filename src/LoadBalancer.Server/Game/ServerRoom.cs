using LoadBalancer.Common;
using LoadBalancer.Game;
using LoadBalancer.Server.Common;

namespace LoadBalancer.Server.Game
{
    public partial class ServerRoom : BaseRoom<ServerPlayer, ServerRoomObject>/*, IGameObject*/, IDisposable
    {
        public new RoomProperties Properties { get => (RoomProperties)base.Properties; }
        protected override BaseRoomProperties BuildProperties() => new RoomProperties();

        public event Action OnStart;
        public event Action OnStop;

        public readonly IServerThread Thread = new ServerThread();

        public void Start()
        {
            ((ServerThread)Thread).Start();
            OnStart?.Invoke();
        }

        public void Stop()
        {
            Thread.EnqueueNew(() => OnStop?.Invoke());
            ((ServerThread)Thread).Stop();
        }

        #region Dispose

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    Stop();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~ServerRoom()
        {
            Dispose(false);
        }

        #endregion

        public void AddPlayer(ServerPlayer player, bool raise, IEnumerable<ServerPlayer> playersToNotify)
        {
            Players.Add(player, raise);

            var evt = new JoinRoomParameters { RoomId = RoomId, PlayerId = player.PlayerId, PlayerProperties = player.Properties };
            Notify(playersToNotify, p => p.Handler.OnRoomJoined(evt));
        }

        public void RemovePlayer(ServerPlayer player, bool raise, IEnumerable<ServerPlayer> playersToNotify)
        {
            Players.Remove(player, raise);

            var evt = new LeaveRoomParameters { RoomId = RoomId, PlayerId = player.PlayerId };
            Notify(playersToNotify, p => p.Handler.OnRoomLeaved(evt));
        }

        public void UpdateProperties(KeyValueCollection properties, bool raise, IEnumerable<ServerPlayer> playersToNotify)
        {
            Properties.Merge(properties);

            if (raise || SharedSettings.RaiseLocalEvents)
                RaisePropertiesChanged(properties);

            if (properties.Count > 0)
            {
                var evt = new UpdateRoomParameters { RoomId = RoomId, RoomProperties = properties };
                Notify(playersToNotify, p => p.Handler.OnRoomUpdated(evt));
            }
        }

        public void AddObject(ServerRoomObject obj, bool raise, IEnumerable<ServerPlayer> playersToNotify)
        {
            Objects.Add(obj, raise);

            var evt = new SpawnObjectParameters { RoomId = RoomId, ObjectId = obj.ObjectId, Tag = obj.Tag, ObjectProperties = obj.Properties };
            Notify(playersToNotify, p => p.Handler.OnObjectSpawned(evt));
        }

        protected void Notify(IEnumerable<ServerPlayer> players, Action<ServerPlayer> action)
        {
            if (players == null)
                players = Players;

            foreach (var recipient in players)
                action(recipient);
        }

        //bool IGameObject.IsConnected => Thread.IsStarted;
        //KeyValueCollection IGameObject.Properties => Properties;
        //void IGameObject.UpdateProperties(KeyValueCollection properties) => UpdateProperties(properties, Players.None);
    }
}
