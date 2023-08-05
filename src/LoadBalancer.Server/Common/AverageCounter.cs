using System.Collections.Concurrent;

namespace LoadBalancer.Server.Common
{
    class AverageCounter
    {
        private readonly ConcurrentQueue<int> queue = new ConcurrentQueue<int>();

        private int maxLength;

        public AverageCounter(int maxLength = 100)
        {
            this.maxLength = maxLength;
            queue.Enqueue(0);
        }

        public void Add(int value)
        {
            queue.Enqueue(value);
            while (queue.Count > maxLength) queue.TryDequeue(out _);
        }

        public int Average => (int)queue.Average();

        public override string ToString() => $"{Average} of {queue.Count}";
    }
}
