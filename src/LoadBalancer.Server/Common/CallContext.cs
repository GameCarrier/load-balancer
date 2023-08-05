using LoadBalancer.Common;
using LoadBalancer.Extensions;

namespace LoadBalancer.Server.Common
{
    public interface _IInternalCallContext
    {
        CallResult ContinueExecution(ActionQueue actionQueue, Action action);
        CallResult ContinueExecution(ActionQueue actionQueue, Func<Task> action);
    }

    public class CallContext : ICallContext, _IInternalCallContext
    {
        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<CallContext>();

        private _IInternalHandler Handler;
        public OperationType Type { get; private set; }

        // Realtime block
        public int Code { get; private set; }
        public BinaryReader ParametersReader { get; private set; }

        // Methods & Events
        public long Counter { get; private set; }
        public KeyType Name { get; private set; }
        public KeyValueCollection Parameters { get; private set; }

        public bool IsAccepted { get; private set; }
        public bool IsFinished { get; private set; }

        private ActionQueue completeInQueue;
        private Action completeInAction;

        HandlerTable ICallContext.Metadata { get; set; }
        public string OperationName { get; set; }

        public CallContext(_IInternalHandler handler, OperationType type, int code, BinaryReader reader)
        {
            Handler = handler;
            Type = type;
            Code = code;
            ParametersReader = reader;
        }

        public CallContext(_IInternalHandler handler, OperationType type, long counter, KeyType name, KeyValueCollection parameters)
        {
            Handler = handler;
            Type = type;
            Counter = counter;
            Name = name;
            Parameters = parameters;
        }

        public override string ToString() => $"{Type}: {Code} / {Name} {OperationName} #{Counter}";

        CallResult _IInternalCallContext.ContinueExecution(ActionQueue actionQueue, Action action)
        {
            Action anchorAction = () => { actionQueue.Enqueue(action); };

            completeInQueue = actionQueue;
            completeInAction = anchorAction;

            bool enqueued = actionQueue.Enqueue(() =>
            {
                try
                {
                    action();

                    if (!IsFinished
                        && ReferenceEquals(completeInQueue, actionQueue)
                        && ReferenceEquals(completeInAction, anchorAction)
                        && Type == OperationType.Method)
                        Fail(CommonErrors.Error_ServerException, CommonMessages.Message_NotCompleted);
                }
                catch (ResultException ex)
                {
                    Logger.LogError(ex, "Exception in ContinueExecution");
                    if (!IsFinished && Type == OperationType.Method)
                        Fail(ex.Status, ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Exception in ContinueExecution");
                    if (!IsFinished && Type == OperationType.Method)
                        Fail(CommonErrors.Error_ServerException, ex.Message);
                }
            });

            if (!enqueued && !IsFinished && Type == OperationType.Method)
                Fail(CommonErrors.Error_ServerException, CommonMessages.Message_NotEnqueued);

            return enqueued ? CallResult.Reenqueued : CallResult.Failed;
        }

        CallResult _IInternalCallContext.ContinueExecution(ActionQueue actionQueue, Func<Task> action)
        {
            Action anchorAction = () => { actionQueue.Enqueue(action); };

            completeInQueue = actionQueue;
            completeInAction = anchorAction;

            bool enqueued = actionQueue.Enqueue(async () =>
            {
                try
                {
                    await action();

                    if (!IsFinished
                        && ReferenceEquals(completeInQueue, actionQueue)
                        && ReferenceEquals(completeInAction, anchorAction)
                        && Type == OperationType.Method)
                        Fail(CommonErrors.Error_ServerException, CommonMessages.Message_NotCompleted);
                }
                catch (ResultException ex)
                {
                    Logger.LogError(ex, "Exception in ContinueExecution");
                    if (!IsFinished && Type == OperationType.Method)
                        Fail(ex.Status, ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Exception in ContinueExecution");
                    if (!IsFinished && Type == OperationType.Method)
                        Fail(CommonErrors.Error_ServerException, ex.Message);
                }
            });

            if (!enqueued && !IsFinished && Type == OperationType.Method)
                Fail(CommonErrors.Error_ServerException, CommonMessages.Message_NotEnqueued);

            return enqueued ? CallResult.Reenqueued : CallResult.Failed;
        }

        public void Accept() => IsAccepted = true;

        public CallResult Fail(Enum status, string message = null)
        {
            if (Type == OperationType.Realtime)
                throw new ResultException(status, message);

            if (Type == OperationType.Event)
                throw new ResultException(status, message);

            return Fail(Result.Error(status, message));
        }

        private CallResult Fail(KeyType status, string message = null)
        {
            if (Type == OperationType.Realtime)
                throw new ResultException(status, message);

            if (Type == OperationType.Event)
                throw new ResultException(status, message);

            return Fail(Result.Error(status, message));
        }

        public CallResult Fail(Result result)
        {
            if (Type == OperationType.Realtime)
                throw new ResultException(result.Status, result.Message);

            if (Type == OperationType.Event)
                throw new ResultException(result.Status, result.Message);

            return Fail(result.Serialize());
        }

        public CallResult Fail(KeyValueCollection parameters = null)
        {
            if (Type == OperationType.Realtime)
                throw new ResultException(parameters ?? new KeyValueCollection());

            if (Type == OperationType.Event)
                throw new ResultException(parameters ?? new KeyValueCollection());

            if (Type == OperationType.Method)
                Handler.Send(OperationType.Method, Counter, Name, parameters);

            IsFinished = true;
            return CallResult.Failed;
        }

        public CallResult Complete(Result result)
        {
            return Complete(result.Serialize());
        }

        public CallResult Complete(KeyValueCollection parameters = null)
        {
            if (Type == OperationType.Method)
                Handler.Send(OperationType.Method, Counter, Name, parameters);

            IsFinished = true;
            return CallResult.Completed;
        }
    }
}
