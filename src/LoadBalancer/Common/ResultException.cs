using LoadBalancer.Extensions;
using System;
using System.Collections.Generic;

namespace LoadBalancer.Common
{
    public class ResultException : Exception
    {
        public KeyType Status { get; private set; } = KeyType.Empty;
        public List<string> CallContext { get; private set; }

        public ResultException(KeyType status, string message = null) : base(message) { Status = status; }
        public ResultException(Result result) : base(result.Message) { Status = result.Status; }
        public ResultException(KeyValueCollection parameters) : this(parameters.Materialize<Result>()) { }

        public ResultException AddCallContext(string str)
        {
            if (CallContext == null)
                CallContext = new List<string>();

            CallContext.Insert(0, str);
            return this;
        }

        public override string Message => base.Message
            + (CallContext != null ? "\n" + string.Join("\n", CallContext) : null);
    }
}
