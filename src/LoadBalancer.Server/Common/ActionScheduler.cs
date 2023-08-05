namespace LoadBalancer.Server.Common
{
    public interface IActionExecutor
    {
        Action<Action> ExecuteAction { get; set; }
        Action<Func<Task>> ExecuteTask { get; set; }
    }

    public interface IScheduledItem : IActionExecutor, IDisposable
    {
        DateTime NextRun { get; }
        int? Interval { get; }
        bool Suspended { get; }
        bool Executed { get; }
        bool Disposed { get; }
        void Suspend();
        void Resume(int? timeoutMs = null);
    }

    public class ActionScheduler : IActionExecutor, IDisposable
    {
        #region Inner classes

        class SortedQueue<T>
        {
            private readonly LinkedList<T> queue = new();

            private readonly IComparer<T> comparer;

            public SortedQueue()
            {
                comparer = Comparer<T>.Default;
            }

            public SortedQueue(IComparer<T> comparer)
            {
                this.comparer = comparer;
            }

            public T FirstOrDefault()
            {
                var node = queue.First;
                return node == null ? default : node.Value;
            }

            public void Enqueue(T value)
            {
                var node = queue.First;
                while (node != null && comparer.Compare(node.Value, value) <= 0)
                    node = node.Next;
                if (node == null)
                    queue.AddLast(value);
                else
                    queue.AddBefore(node, value);
            }

            public bool TryDequeue(out T value)
            {
                value = default;
                var node = queue.First;
                if (node == null)
                    return false;

                value = node.Value;
                queue.RemoveFirst();
                return true;
            }

            public bool TryRemove(T value) => queue.Remove(value);
        }

        class ScheduledItem : IScheduledItem
        {
            public ActionScheduler Scheduler { get; set; }
            public DateTime NextRun { get; set; }
            public Action Action { get; set; }
            public Func<Task> Task { get; set; }
            public int Timeout { get; set; }
            public int? Interval { get; set; }
            public Action<Action> ExecuteAction { get; set; }
            public Action<Func<Task>> ExecuteTask { get; set; }
            public bool Suspended { get; set; }
            public bool Executed { get; private set; }
            public bool Disposed { get; private set; }

            public void Suspend()
            {
                if (Suspended) return;
                Suspended = true;
                Scheduler.SchedulerThread.SuspendItem(this);
            }

            public void Resume(int? timeoutMs = null)
            {
                if (!Suspended) return;
                Suspended = false;
                Scheduler.SchedulerThread.ResumeItem(this, timeoutMs);
            }

            public void Execute() => Executed = true;
            public void Dispose()
            {
                Disposed = true;
                Scheduler.OnItemDisposed(this);
            }
        }

        class ScheduledItemComparer : IComparer<ScheduledItem>
        {
            public int Compare(ScheduledItem x, ScheduledItem y)
            {
                return x.NextRun.CompareTo(y.NextRun);
            }
        }

        class ActionSchedulerThread
        {
            static ActionSchedulerThread()
            {
                Instance.Start();
            }

            public static readonly ActionSchedulerThread Instance = new ActionSchedulerThread();

            private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<ActionSchedulerThread>();

            private readonly object lockObject = new object();

            private readonly SortedQueue<ScheduledItem> scheduledItems = new SortedQueue<ScheduledItem>(
                new ScheduledItemComparer());

            private ScheduledItem FirstScheduledItem { get { lock (lockObject) return scheduledItems.FirstOrDefault(); } }

            private ScheduledItem waitingItem;
            private readonly AutoResetEvent resetEvent = new AutoResetEvent(false);
            private Thread thread;

            private bool isStarted;
            public bool IsStarted
            {
                get { lock (lockObject) return isStarted; }
                private set { lock (lockObject) isStarted = value; }
            }

            public void Start()
            {
                if (IsStarted) return;
                IsStarted = true;
                thread = new Thread(SchedulerLoop) { IsBackground = true };
                thread.Start();
            }

            public void Stop()
            {
                if (!IsStarted) return;
                IsStarted = false;
            }

            private void SchedulerLoop()
            {
                while (IsStarted)
                {
                    var item = FirstScheduledItem;
                    if (item == null)
                    {
                        resetEvent.WaitOne(500);
                        continue;
                    }

                    lock (lockObject) waitingItem = item;
                    // wait for next item
                    int remain = (int)item.NextRun.Subtract(DateTime.UtcNow).TotalMilliseconds;
                    if (remain > 0 && resetEvent.WaitOne(remain))
                        continue;   // other item come

                    if (!IsStarted) return;

                    bool wasExecuted = false;
                    try
                    {
                        if (IsStarted && item.Scheduler.IsStarted && !item.Disposed)
                            if (ReferenceEquals(item, FirstScheduledItem))
                            {
                                item.Execute();
                                if (item.Action != null)
                                    (item.ExecuteAction ?? item.Scheduler.ExecuteAction)?.Invoke(item.Action);
                                else
                                if (item.Task != null)
                                    (item.ExecuteTask ?? item.Scheduler.ExecuteTask)?.Invoke(item.Task);
                                wasExecuted = true;
                            }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Exception in SchedulerLoop");
                        wasExecuted = true;    // proceed to avoid loop on exception item
                    }

                    lock (lockObject) waitingItem = null;

                    if (!IsStarted) return;

                    lock (lockObject)
                    {
                        if (wasExecuted || !item.Scheduler.IsStarted || item.Disposed)
                            scheduledItems.TryRemove(item);

                        if (wasExecuted && item.Scheduler.IsStarted && !item.Disposed && item.Interval != null)
                        {
                            item.NextRun = item.NextRun.AddMilliseconds(item.Interval.Value);
                            scheduledItems.Enqueue(item);
                        }
                    }
                }
            }

            public void Enqueue(ScheduledItem item)
            {
                if (!IsStarted) return;
                lock (lockObject)
                {
                    scheduledItems.Enqueue(item);
                    var firstItem = FirstScheduledItem;
                    if (firstItem == null) return;
                    if (waitingItem == null || !ReferenceEquals(waitingItem, firstItem))
                        resetEvent.Set();
                }
            }

            public void OnItemDisposed(ScheduledItem item)
            {
                if (!IsStarted) return;
                lock (lockObject)
                {
                    if (waitingItem != null && ReferenceEquals(waitingItem, item))
                        resetEvent.Set();
                }
            }

            public void OnStopped(ActionScheduler scheduler)
            {
                if (!IsStarted) return;
                lock (lockObject)
                {
                    if (waitingItem != null && ReferenceEquals(waitingItem.Scheduler, scheduler))
                        resetEvent.Set();
                }
            }

            #region Suspend / Resume

            public void SuspendItem(ScheduledItem item)
            {
                if (!IsStarted) return;
                lock (lockObject)
                {
                    scheduledItems.TryRemove(item);
                    resetEvent.Set();
                }
            }

            public void ResumeItem(ScheduledItem item, int? timeoutMs)
            {
                if (item.Disposed) return;
                if (!IsStarted) return;

                if (timeoutMs != null)
                    item.NextRun = DateTime.UtcNow.AddMilliseconds(timeoutMs.Value);
                else
                if (!item.Executed)
                    item.NextRun = DateTime.UtcNow.AddMilliseconds(item.Timeout);
                else
                if (item.Executed && item.Interval != null)
                    item.NextRun = DateTime.UtcNow.AddMilliseconds(item.Interval.Value);
                else
                    return;

                Enqueue(item);
            }

            #endregion
        }

        #endregion

        public ActionScheduler() => this.UseSynchronousExecutor();

        private readonly object lockObject = new object();

        private readonly ActionSchedulerThread SchedulerThread = ActionSchedulerThread.Instance;

        private bool isStarted;
        public bool IsStarted
        {
            get { lock (lockObject) return isStarted; }
            private set { lock (lockObject) isStarted = value; }
        }

        #region IActionExecutor

        public Action<Action> ExecuteAction { get; set; }
        public Action<Func<Task>> ExecuteTask { get; set; }

        #endregion

        #region Start / Stop

        public void Start()
        {
            if (IsStarted) return;
            IsStarted = true;
        }

        public void Stop()
        {
            if (!IsStarted) return;
            IsStarted = false;
            SchedulerThread.OnStopped(this);
        }

        #endregion

        #region Schedule

        public IScheduledItem Schedule(Action action, int timeoutMs, int? intervalMs = null, bool suspended = false)
        {
            if (!IsStarted) return null;
            var next = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            var item = new ScheduledItem { Scheduler = this, Action = action, Timeout = timeoutMs, Interval = intervalMs, Suspended = suspended, NextRun = next };

            if (!item.Suspended)
                SchedulerThread.Enqueue(item);

            return item;
        }

        public IScheduledItem Schedule<T>(Func<T> action, int timeoutMs, int? intervalMs = null, bool suspended = false) =>
            Schedule(() => { action(); }, timeoutMs, intervalMs, suspended);

        public IScheduledItem Schedule(Func<Task> action, int timeoutMs, int? intervalMs = null, bool suspended = false)
        {
            if (!IsStarted) return null;
            var next = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            var item = new ScheduledItem { Scheduler = this, Task = action, Timeout = timeoutMs, Interval = intervalMs, Suspended = suspended, NextRun = next };

            if (!item.Suspended)
                SchedulerThread.Enqueue(item);

            return item;
        }

        public IScheduledItem Schedule<T>(Func<Task<T>> action, int timeoutMs, int? intervalMs = null, bool suspended = false) =>
            Schedule(() => (Task)action(), timeoutMs, intervalMs, suspended);

        #endregion

        private void OnItemDisposed(ScheduledItem item) => SchedulerThread.OnItemDisposed(item);

        public void Dispose()
        {
            Stop();
        }
    }

    public static class ActionSchedulerExtensions
    {
        public static T UseSynchronousExecutor<T>(this T item) where T : IActionExecutor
        {
            item.ExecuteAction = action => action();
            item.ExecuteTask = action => action();
            return item;
        }

        public static T UseThreadPoolExecutor<T>(this T item) where T : IActionExecutor
        {
            item.ExecuteAction = action => ThreadPool.QueueUserWorkItem(_ => action());
            item.ExecuteTask = action => action();  // Task works here
            return item;
        }

        public static T UseActionQueueExecutor<T>(this T item, ActionQueue queue) where T : IActionExecutor
        {
            item.ExecuteAction = action => queue.Enqueue(action);
            item.ExecuteTask = action => queue.Enqueue(action);
            return item;
        }
    }
}
