using LoadBalancer.Common;
using LoadBalancer.Extensions;
using System.IO;

namespace LoadBalancer.Client.Common
{
    partial class ClientCallContext : ICallContext
    {
        public OperationType Type { get; private set; }

        // Realtime block
        public int Code { get; private set; }
        public BinaryReader ParametersReader { get; private set; }

        // Methods & Events
        public long Counter { get; private set; }
        public KeyType Name { get; private set; }
        public KeyValueCollection Parameters { get; private set; }

        public bool IsAccepted { get; private set; }
        public bool IsCompleted { get; private set; }

        HandlerTable ICallContext.Metadata { get; set; }
        public string OperationName { get; set; }

        public ClientCallContext(OperationType type, int code, BinaryReader reader)
        {
            Type = type;
            Code = code;
            ParametersReader = reader;
        }

        public ClientCallContext(OperationType type, long counter, KeyType name, KeyValueCollection parameters)
        {
            Type = type;
            Counter = counter;
            Name = name;
            Parameters = parameters;
        }

        public override string ToString() => $"{Type}: {Code} / {Name} {OperationName}";

        public void Accept() => IsAccepted = true;

        public CallResult Fail(KeyType status, string message = null)
        {
            IsCompleted = true;
            throw new ResultException(status, message);
        }

        public CallResult Fail(Result result)
        {
            IsCompleted = true;
            throw new ResultException(result.Status, result.Message);
        }

        public CallResult Fail(KeyValueCollection parameters = null)
        {
            IsCompleted = true;
            throw new ResultException(parameters ?? new KeyValueCollection());
        }

        public CallResult Complete()
        {
            IsCompleted = true;
            return CallResult.Completed;
        }
    }
}
