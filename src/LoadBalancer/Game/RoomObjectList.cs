using LoadBalancer.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LoadBalancer.Game
{
    public interface IRoomObjectList<out O> : IEnumerable<O>
    {
        int Count { get; }
        O this[string objectId] { get; }

        event Action<O> OnSpawned;
        event Action<O> OnDestroyed;
    }

    public class RoomObjectList<O> : IEnumerable<O>, IRoomObjectList<O> where O : BaseRoomObject
    {
        private readonly object lockObject = new object();
        private readonly List<O> list = new List<O>();
        public BaseRoom Room { get; set; }
        public event Action<O> OnSpawned;
        public event Action<O> OnDestroyed;
        public int Count { get { lock (lockObject) return list.Count; } }

        public void Add(O obj, bool raise = true)
        {
            obj.Room = Room;
            lock (lockObject) list.Add(obj);
            if (raise || SharedSettings.RaiseLocalEvents)
                OnSpawned?.Invoke(obj);
        }

        public void Remove(O obj, bool raise = true)
        {
            lock (lockObject) list.Remove(obj);
            if (raise || SharedSettings.RaiseLocalEvents)
                OnDestroyed?.Invoke(obj);
            obj.Room = null;
        }

        public void Clear(bool raise = true)
        {
            lock (lockObject)
            {
                foreach (var obj in list.ToList())
                    Remove(obj, raise);
            }
        }

        public O this[string objectId]
        {
            get { lock (lockObject) return list.FirstOrDefault(p => p.ObjectId == objectId); }
        }

        private List<O> All() { lock (lockObject) return list.ToList(); }

        public IEnumerator<O> GetEnumerator() => All().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => All().GetEnumerator();

        public override string ToString() => $"{Count} items";
    }
}
