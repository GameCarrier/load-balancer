namespace LoadBalancer.Server.Common
{
    public class ActionQueue : IDisposable
    {
        class ActionItem
        {
            public Action Action;
            public Func<Task> Task;
        }

        public enum ActionExecutionModel { ThreadPool, Task }

        public ActionExecutionModel ExecutionModel { get; set; } = ActionExecutionModel.ThreadPool;

        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<ActionQueue>();

        private readonly object lockObject = new object();

        private readonly Queue<ActionItem> queueActions = new Queue<ActionItem>();

        private bool isStarted;
        public bool IsStarted
        {
            get { lock (lockObject) return isStarted; }
            private set { lock (lockObject) isStarted = value; }
        }

        private ActionItem currentItem;

        public void Start()
        {
            if (IsStarted) return;
            IsStarted = true;
            ProcessNextAction();
        }

        public void Stop()
        {
            if (!IsStarted) return;
            IsStarted = false;
            ProcessNextAction();
        }

        public bool Enqueue(Action action)
        {
            if (!IsStarted) return false;

            var item = new ActionItem { Action = action };
            lock (lockObject) queueActions.Enqueue(item);

            ProcessNextAction();
            return true;
        }

        public bool Enqueue<T>(Func<T> action) => Enqueue(() => { action(); });

        public bool Enqueue(Func<Task> action)
        {
            if (!IsStarted) return false;

            var item = new ActionItem { Task = action };
            lock (lockObject) queueActions.Enqueue(item);

            ProcessNextAction();
            return true;
        }

        public bool Enqueue<T>(Func<Task<T>> action) => Enqueue(() => (Task)action());

        private bool TryDequeueNextAction(out ActionItem item)
        {
            lock (lockObject)
                return queueActions.TryDequeue(out item);
        }

        private void ProcessNextAction()
        {
            lock (lockObject)
            {
                if (currentItem != null) return;

                if (!TryDequeueNextAction(out var nextAction)) return;

                Execute(nextAction);
            }
        }

        private void Execute(ActionItem item)
        {
            lock (lockObject) currentItem = item;

            if (item.Action != null)
            {
                var body = () =>
                {
                    try
                    {
                        item.Action();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Exception in Execute");
                    }

                    lock (lockObject) currentItem = null;

                    ProcessNextAction();
                };

                if (ExecutionModel == ActionExecutionModel.ThreadPool)
                    ThreadPool.QueueUserWorkItem(_ => body());
                else
                if (ExecutionModel == ActionExecutionModel.Task)
                    Task.Run(body);
            }
            else
            if (item.Task != null)
            {
                var body = async () =>
                {
                    try
                    {
                        var task = item.Task();
                        await task;
                        lock (lockObject) currentItem = null;
                        ProcessNextAction();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Exception in Task action");
                        lock (lockObject) currentItem = null;
                        ProcessNextAction();
                    }
                };
                body();
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
