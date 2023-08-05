using LoadBalancer.Client.Common;

namespace LoadBalancer.Server.Common
{
    public interface IServerThread
    {
        bool IsStarted { get; }
        bool EnqueueNew(Action action);
        bool EnqueueNew<T>(Func<T> action);
        bool EnqueueNew(Func<Task> action);
        bool EnqueueNew<T>(Func<Task<T>> action);

        bool EnqueueNew_IfConnected(Action action);
        bool EnqueueNew_IfConnected<T>(Func<T> action);
        bool EnqueueNew_IfConnected(Func<Task> action);
        bool EnqueueNew_IfConnected<T>(Func<Task<T>> action);

        CallResult Enqueue(CallContext call, Action action);
        CallResult Enqueue<T>(CallContext call, Func<T> action);
        CallResult Enqueue(CallContext call, Func<Task> action);
        CallResult Enqueue<T>(CallContext call, Func<Task<T>> action);

        IScheduledItem Schedule(Action action, int timeoutMs, int? intervalMs = null, bool suspended = false);
        IScheduledItem Schedule<T>(Func<T> action, int timeoutMs, int? intervalMs = null, bool suspended = false);
        IScheduledItem Schedule(Func<Task> action, int timeoutMs, int? intervalMs = null, bool suspended = false);
        IScheduledItem Schedule<T>(Func<Task<T>> action, int timeoutMs, int? intervalMs = null, bool suspended = false);
    }

    class ServerThread : IServerThread, IDisposable
    {
        private readonly ActionQueue ActionQueue = new ActionQueue();
        private readonly ActionScheduler ActionScheduler = new ActionScheduler();
        public bool IsStarted => ActionQueue.IsStarted;
        public IServiceConnect Connect { get; set; }

        public void Start()
        {
            ActionScheduler.UseActionQueueExecutor(ActionQueue);
            ActionQueue.Start();
            ActionScheduler.Start();
        }

        public bool EnqueueNew(Action action) => ActionQueue.Enqueue(action);
        public bool EnqueueNew<T>(Func<T> action) => EnqueueNew(() => { action(); });
        public bool EnqueueNew(Func<Task> action) => ActionQueue.Enqueue(action);
        public bool EnqueueNew<T>(Func<Task<T>> action) => EnqueueNew(() => (Task)action());

        public bool EnqueueNew_IfConnected(Action action)
        {
            if (!Connect.IsConnected) return false;
            return ActionQueue.Enqueue(() =>
            {
                if (!Connect.IsConnected) return false;
                action();
                return true;
            });
        }
        public bool EnqueueNew_IfConnected<T>(Func<T> action) => EnqueueNew_IfConnected(() => { action(); });
        public bool EnqueueNew_IfConnected(Func<Task> action)
        {
            if (!Connect.IsConnected) return false;
            return ActionQueue.Enqueue(async () =>
            {
                if (!Connect.IsConnected) return false;
                await action();
                return true;
            });
        }
        public bool EnqueueNew_IfConnected<T>(Func<Task<T>> action) => EnqueueNew_IfConnected(() => (Task)action());

        public CallResult Enqueue(CallContext call, Action action) =>
            ((_IInternalCallContext)call).ContinueExecution(ActionQueue, action);
        public CallResult Enqueue<T>(CallContext call, Func<T> action) => Enqueue(call, () => { action(); });
        public CallResult Enqueue(CallContext call, Func<Task> action) =>
            ((_IInternalCallContext)call).ContinueExecution(ActionQueue, action);
        public CallResult Enqueue<T>(CallContext call, Func<Task<T>> action) => Enqueue(call, () => (Task)action());

        public IScheduledItem Schedule(Action action, int timeoutMs, int? intervalMs = null, bool suspended = false) =>
            ActionScheduler.Schedule(action, timeoutMs, intervalMs, suspended);
        public IScheduledItem Schedule<T>(Func<T> action, int timeoutMs, int? intervalMs = null, bool suspended = false) =>
            Schedule(() => { action(); }, timeoutMs, intervalMs, suspended);
        public IScheduledItem Schedule(Func<Task> action, int timeoutMs, int? intervalMs = null, bool suspended = false) =>
            ActionScheduler.Schedule(action, timeoutMs, intervalMs, suspended);
        public IScheduledItem Schedule<T>(Func<Task<T>> action, int timeoutMs, int? intervalMs = null, bool suspended = false) =>
            Schedule(() => (Task)action(), timeoutMs, intervalMs, suspended);

        public void Stop()
        {
            ActionQueue.Stop();
            ActionScheduler.Stop();
        }

        public void Dispose()
        {
            ActionQueue.Dispose();
            ActionScheduler.Dispose();
        }
    }
}
