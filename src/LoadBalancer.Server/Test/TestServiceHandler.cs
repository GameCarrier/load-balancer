using LoadBalancer.Common;
using LoadBalancer.Server.Common;
using LoadBalancer.Test;

namespace LoadBalancer.Server.Test
{
    [MethodsEnum(typeof(TestMethods))]
    [ErrorsEnum(typeof(TestErrors))]
    internal class TestServiceHandler : HandlerBase
    {
        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<TestServiceHandler>();

        public new TestService Service => (TestService)base.Service;

        protected override void OnRealtimeOperation(CallContext call)
        {
            Logger.LogInformation($"OnRealtimeOperation {call.Code}");
            switch (call.Code)
            {
                case 0:
                    break;

                case 1:

                    int count = call.ParametersReader.ReadInt32();

                    SendRealtime(1, writer =>
                    {
                        writer.Write(count);
                        for (int i = 0; i < count; i++)
                        {
                            int num = call.ParametersReader.ReadInt32();
                            writer.Write(num);
                        }
                    });

                    break;
            }
        }

        protected CallResult Incompleted(CallContext call) => CallResult.NotHandled;
        protected CallResult NestedIncompleted(CallContext call) => Thread.Enqueue(call, () => false);
        protected CallResult Exception(CallContext call) => throw new NotImplementedException("not implemented exception");
        protected CallResult ResultException(CallContext call) => throw new ResultException(TestErrors.Error_Test, "result exception");
        protected CallResult NestedException(CallContext call) => Thread.Enqueue(call, () => throw new NotImplementedException("nested not implemented exception"));
        protected CallResult NestedResultException(CallContext call) => Thread.Enqueue(call, () => throw new ResultException(TestErrors.Error_Test, "nested result exception"));
        protected CallResult MarkSendFromDisconnect(CallContext call)
        {
            sendFromDisconnect = call.Parameters.GetValue<int>(TestKeys.Count);
            return call.Complete();
        }

        private int sendFromDisconnect;
        protected override void OnDisconnected(CallContext call)
        {
            if (sendFromDisconnect > 0)
            {
                var others = Service.Handlers.Where(h => !ReferenceEquals(h, this)).ToList();
                for (int i = 0; i < sendFromDisconnect; i++)
                    foreach (TestServiceHandler handler in others)
                        handler.RaiseEvent(TestMethods.CallFromDisconnect);
            }
        }
    }
}
