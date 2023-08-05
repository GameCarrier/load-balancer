using LoadBalancer.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LoadBalancer.Game
{
    public class RoomList<R> : IEnumerable<R> where R : BaseRoom
    {
        private readonly object lockObject = new object();
        private readonly List<R> list = new List<R>();

        public event Action<R> OnRoomAdded;
        public event Action<R> OnRoomRemoved;

        public int Count { get { lock (lockObject) return list.Count; } }

        public void Add(R room, bool raise = true)
        {
            lock (lockObject) list.Add(room);
            if (raise || SharedSettings.RaiseLocalEvents)
                OnRoomAdded?.Invoke(room);
        }

        public void Remove(R room, bool raise = true)
        {
            lock (lockObject) list.Remove(room);
            if (raise || SharedSettings.RaiseLocalEvents)
                OnRoomRemoved?.Invoke(room);
        }

        public void Clear(bool raise = true)
        {
            lock (lockObject)
            {
                foreach (var room in list.ToList())
                    Remove(room, raise);
            }
        }
        public R this[string roomId]
        {
            get { lock (lockObject) return list.FirstOrDefault(p => p.RoomId == roomId); }
        }

        private List<R> All() { lock (lockObject) return list.ToList(); }

        public IEnumerator<R> GetEnumerator() => All().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => All().GetEnumerator();

        public override string ToString() => $"{Count} items";
    }
}
