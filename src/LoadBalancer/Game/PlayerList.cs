using LoadBalancer.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LoadBalancer.Game
{
    public interface IPlayerList<out P> : IEnumerable<P>
    {
        int Count { get; }
        P this[string playerId] { get; }

        event Action<P> OnJoin;
        event Action<P> OnLeave;
    }

    public class PlayerList<P> : IEnumerable<P>, IPlayerList<P> where P : BasePlayer
    {
        private readonly object lockObject = new object();
        private readonly List<P> list = new List<P>();
        public BaseRoom Room { get; set; }
        public event Action<P> OnJoin;
        public event Action<P> OnLeave;
        public int Count { get { lock (lockObject) return list.Count; } }

        public void Add(P player, bool raise = true)
        {
            player.Room = Room;
            lock (lockObject) list.Add(player);
            if (raise || SharedSettings.RaiseLocalEvents)
                OnJoin?.Invoke(player);
        }

        public void Remove(P player, bool raise = true)
        {
            lock (lockObject) list.Remove(player);
            if (raise || SharedSettings.RaiseLocalEvents)
                OnLeave?.Invoke(player);
            player.Room = null;
        }

        public void Clear(bool raise = true)
        {
            lock (lockObject)
            {
                foreach (var player in list.ToList())
                    Remove(player, raise);
            }
        }

        public P this[string playerId]
        {
            get { lock (lockObject) return list.FirstOrDefault(p => p.PlayerId == playerId); }
        }

        public List<P> None => new List<P>();

        private List<P> All() { lock (lockObject) return list.ToList(); }

        public IEnumerator<P> GetEnumerator() => All().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => All().GetEnumerator();

        public override string ToString() => $"{Count} items";
    }

    public static class PlayerListExtensions
    {
        public static IEnumerable<P> Only<P>(this IEnumerable<P> list, string includePlayerId)
            where P : BasePlayer => list.Where(p => includePlayerId == null || p.PlayerId == includePlayerId);

        public static IEnumerable<P> Except<P>(this IEnumerable<P> list, string exceptPlayerId)
            where P : BasePlayer => list.Where(p => p.PlayerId != exceptPlayerId);
    }
}
