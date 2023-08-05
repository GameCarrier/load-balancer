using System.Collections;

namespace LoadBalancer.Server.Common
{
    public class HandlerList : IEnumerable<HandlerBase>
    {
        private readonly object lockObject = new object();

        private readonly Stack<int> freeIndexes = new Stack<int>();
        private readonly List<HandlerBase> list = new List<HandlerBase> { null }; // reserve 0-index

        public int Count { get { lock (lockObject) return list.Count; } }

        public T FindHandler<T>(int index, Func<T, bool> predicate = null) where T : HandlerBase
        {
            lock (lockObject)
            {
                if (index < list.Count)
                {
                    var handler = list[index] as T;
                    if (handler != null && predicate(handler))
                        return handler;
                }
            }

            return null;
        }

        public List<T> FindHandlers<T>(Func<T, bool> predicate) where T : HandlerBase
        {
            lock (lockObject)
                return list.OfType<T>().Where(p => p != null).Where(predicate).ToList();
        }

        public void AddHandler(HandlerBase handler)
        {
            if (handler.Index != 0)
                throw new ArgumentException("Handler index should be 0 on AddHandler");

            lock (lockObject)
            {
                if (freeIndexes.Count > 0)
                {
                    handler.Index = freeIndexes.Pop();
                    list[handler.Index] = handler;
                }
                else
                {
                    handler.Index = list.Count;
                    list.Add(handler);
                }
            }
        }

        public void RemoveHandler(HandlerBase handler)
        {
            if (handler.Index < 0)
                throw new ArgumentException("Handler index can't be negative on RemoveHandler");

            if (handler.Index == 0)
                return;    // skip on removing non-added handler

            lock (lockObject)
            {
                if (handler.Index >= list.Count)
                    throw new ArgumentException("Handler index is out of HandlerList bounds");

                list[handler.Index] = null;
                freeIndexes.Push(handler.Index);
                handler.Index = 0;
            }
        }

        public List<HandlerBase> All()
        {
            lock (lockObject)
                return list.Where(p => p != null).ToList();
        }

        public IEnumerator<HandlerBase> GetEnumerator() => All().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => All().GetEnumerator();

        public override string ToString() => $"{Count} items";
    }
}
