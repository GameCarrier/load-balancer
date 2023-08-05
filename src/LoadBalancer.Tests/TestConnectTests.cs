using LoadBalancer.Client.Common;
using LoadBalancer.Common;
using LoadBalancer.Test;

namespace LoadBalancer.Tests
{
    [TestClass]
    public class TestConnectTests
    {
        private static readonly Endpoint TestEndpoint = Endpoint.Parse("wss://127.0.0.1:7777/test");

        [TestInitialize]
        public void TestInitialize()
        {
            ServiceConnect.InitClientLibraryLogging("TestConnectTests.log");
            ServiceConnect.InitClientLibraryMode(GameCarrier.Clients.GcClientMode.Active);

            Client.Bootstrapper.RegisterTypes();
            Client.Bootstrapper.ConfigureServiceFactory(ServiceFactory.Instance);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ServiceConnect.CleanupClientLibraryMode();
        }

        [TestMethod]
        public async Task Test_OperationOnServer_Incompleted()
        {
            using var connect = new ServiceConnect();

            var result = await connect.Connect(TestEndpoint);
            Assert.IsTrue(result.IsOk);

            result = await connect.CallMethod<Result>(TestMethods.Incompleted);
            Console.WriteLine(result);
            Assert.IsFalse(result.IsOk);
            Assert.AreEqual(CommonMessages.Message_NotCompleted, result.Message);
        }

        [TestMethod]
        public async Task Test_NestedOperationOnServer_Incompleted()
        {
            using var connect = new ServiceConnect();

            var result = await connect.Connect(TestEndpoint);
            Assert.IsTrue(result.IsOk);

            result = await connect.CallMethod<Result>(TestMethods.NestedIncompleted);
            Console.WriteLine(result);
            Assert.IsFalse(result.IsOk);
            Assert.AreEqual(CommonMessages.Message_NotCompleted, result.Message);
        }

        [TestMethod]
        public async Task Test_OperationOnServer_CustomException()
        {
            using var connect = new ServiceConnect();

            var result = await connect.Connect(TestEndpoint);
            Assert.IsTrue(result.IsOk);

            result = await connect.CallMethod<Result>(TestMethods.Exception);
            Console.WriteLine(result);
            Assert.IsFalse(result.IsOk);
            Assert.AreEqual("not implemented exception", result.Message);
        }

        [TestMethod]
        public async Task Test_OperationOnServer_ResultException()
        {
            using var connect = new ServiceConnect();

            var result = await connect.Connect(TestEndpoint);
            Assert.IsTrue(result.IsOk);

            result = await connect.CallMethod<Result>(TestMethods.ResultException);
            Console.WriteLine(result);
            Assert.IsFalse(result.IsOk);
            Assert.AreEqual(TestErrors.Error_Test, result.Status);
            Assert.AreEqual("result exception", result.Message);
        }

        [TestMethod]
        public async Task Test_NestedOperationOnServer_CustomException()
        {
            using var connect = new ServiceConnect();

            var result = await connect.Connect(TestEndpoint);
            Assert.IsTrue(result.IsOk);

            result = await connect.CallMethod<Result>(TestMethods.NestedException);
            Console.WriteLine(result);
            Assert.IsFalse(result.IsOk);
            Assert.AreEqual("nested not implemented exception", result.Message);
        }

        [TestMethod]
        public async Task Test_NestedOperationOnServer_ResultException()
        {
            using var connect = new ServiceConnect();

            var result = await connect.Connect(TestEndpoint);
            Assert.IsTrue(result.IsOk);

            result = await connect.CallMethod<Result>(TestMethods.NestedResultException);
            Console.WriteLine(result);
            Assert.IsFalse(result.IsOk);
            Assert.AreEqual(TestErrors.Error_Test, result.Status);
            Assert.AreEqual("nested result exception", result.Message);
        }

        [TestMethod]
        public async Task Test_RealtimeOperation()
        {
            using var connect = new ServiceConnect();

            Console.WriteLine($"Thread before Connect: {Thread.CurrentThread.ManagedThreadId}");

            var result = await connect.Connect(TestEndpoint);
            Assert.IsTrue(result.IsOk);

            Console.WriteLine($"Thread after Connect: {Thread.CurrentThread.ManagedThreadId}");

            connect.SendRealtime(1, writer =>
            {
                const int count = 10;
                writer.Write(count);
                for (int i = 0; i < count; i++)
                    writer.Write(i);
            });

            int sum = 0;
            int threadId = 0;
            connect.OnRealtimeOperation += call =>
            {
                threadId = Thread.CurrentThread.ManagedThreadId;
                int count = call.ParametersReader.ReadInt32();
                for (int i = 0; i < count; i++)
                    sum += call.ParametersReader.ReadInt32();

                Assert.AreEqual(1, call.Code);
                Assert.AreEqual(10, count);
            };

            await connect.ExpectRealtimeOperation();
            Assert.AreEqual(45, sum);

            Console.WriteLine($"Thread OnRealtimeOperation: {threadId}");
            Console.WriteLine($"Thread after ExpectRealtimeOperation: {Thread.CurrentThread.ManagedThreadId}");
        }

        [TestMethod]
        public void TestLoadFlow()
        {
            var task = LoadFlow();
            while (!task.IsCompleted)
            {
                if (ServiceConnect.CurrentClientLibraryMode != GameCarrier.Clients.GcClientMode.Active)
                    GameCarrier.Clients.Manager.Service();
                Thread.Sleep(100);
            }

            if (task.Exception != null && task.Exception is AggregateException agg)
                Assert.Fail(agg.InnerException.Message + "\n\n" + agg.InnerException.StackTrace);

            if (task.Exception != null)
                Assert.Fail(task.Exception.Message + "\n\n" + task.Exception.StackTrace);
        }

        private async Task LoadFlow()
        {
            using var connect = new ServiceConnect();

            Console.WriteLine($"Thread before Connect: {Thread.CurrentThread.ManagedThreadId}");

            var result = await connect.Connect(TestEndpoint);
            Assert.IsTrue(result.IsOk);

            Console.WriteLine($"Thread after Connect: {Thread.CurrentThread.ManagedThreadId}");

            connect.SendRealtime(0, writer => { });

            int count = 0;
            string disconnectReason = null;
            connect.OnDisconnected += reason => disconnectReason = $"Disconnected by server: {reason}";

            var start = DateTime.UtcNow;

            try
            {
                while (DateTime.UtcNow.Subtract(start).TotalMilliseconds <= 1000)
                {
                    connect.SendRealtime(0, writer => { });
                    count++;
                    await Task.Delay(5);
                }
            }
            catch
            {
                Console.WriteLine($"{count} updates sent");
                Console.WriteLine(disconnectReason);
                throw;
            }

            Console.WriteLine($"{count} updates sent");
        }

        [TestMethod]
        public async Task Test_Disconnect()
        {
            using var connect1 = new ServiceConnect();
            using var connect2 = new ServiceConnect();

            var result = await connect1.Connect(TestEndpoint);
            Assert.IsTrue(result.IsOk);
            result = await connect2.Connect(TestEndpoint);
            Assert.IsTrue(result.IsOk);

            const int ExpectedCount = 10;
            result = await connect1.CallMethod<Result>(TestMethods.MarkSendFromDisconnect, new KeyValueCollection
            {
                { TestKeys.Count, ExpectedCount }
            });
            Assert.IsTrue(result.IsOk);

            int counter = 0;
            bool disconnected = false;
            string reason = null;
            connect2.OnEventReceived += call =>
            {
                if (call.Name == TestMethods.CallFromDisconnect)
                    counter++;
                Console.WriteLine(call);
            };
            connect2.OnDisconnected += r =>
            {
                disconnected = true;
                reason = r;
            };

            await connect1.Disconnect();

            await Task.Delay(500);

            if (disconnected)
                Console.WriteLine($"Disconnected. Reason: '{reason}'");
            Console.WriteLine(counter);

            Assert.IsTrue(connect2.IsConnected);
            Assert.AreEqual(ExpectedCount, counter);
        }
    }
}
