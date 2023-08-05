using LoadBalancer.Auth;
using LoadBalancer.Client.Common;
using LoadBalancer.Common;
using LoadBalancer.Game;

namespace LoadBalancer.Tests
{
    [TestClass]
    public class ClientModeTests_Active : ClientModeTests
    {
        protected override GameCarrier.Clients.GcClientMode CurrentMode => GameCarrier.Clients.GcClientMode.Active;
    }

    [TestClass]
    public class ClientModeTests_Hybrid : ClientModeTests
    {
        protected override GameCarrier.Clients.GcClientMode CurrentMode => GameCarrier.Clients.GcClientMode.Hybrid;
    }

    [TestClass]
    public class ClientModeTests_Passive : ClientModeTests
    {
        protected override GameCarrier.Clients.GcClientMode CurrentMode => GameCarrier.Clients.GcClientMode.Passive;
    }

    public class ClientModeTests
    {
        private static readonly Endpoint AuthEndpoint = Endpoint.Parse("wss://127.0.0.1:7700/auth");
        private static readonly Endpoint GameEndpoint = Endpoint.Parse("wss://127.0.0.1:7731/gameNorthAmerica");

        protected virtual GameCarrier.Clients.GcClientMode CurrentMode => GameCarrier.Clients.GcClientMode.Active;

        [TestInitialize]
        public void TestInitialize()
        {
            ServiceConnect.InitClientLibraryLogging("ClientModeTests.log");
            ServiceConnect.InitClientLibraryMode(CurrentMode);

            Client.Bootstrapper.RegisterTypes();
            Client.Bootstrapper.ConfigureServiceFactory(ServiceFactory.Instance);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ServiceConnect.CleanupClientLibraryMode();
        }

        [TestMethod]
        public void TestSimpleFlow()
        {
            var task = SimpleFlow();
            while (!task.IsCompleted)
            {
                if (CurrentMode != GameCarrier.Clients.GcClientMode.Active)
                    GameCarrier.Clients.Manager.Service();
                Thread.Sleep(100);
            }

            if (task.Exception != null && task.Exception is AggregateException agg)
                Assert.Fail(agg.Message.ToString() + "\n\n" + agg.StackTrace);

            if (task.Exception != null)
                Assert.Fail(task.Exception.Message.ToString() + "\n\n" + task.Exception.StackTrace);
        }

        [TestMethod]
        public void TestLoadFlow()
        {
            var task = LoadFlow();
            while (!task.IsCompleted)
            {
                if (CurrentMode != GameCarrier.Clients.GcClientMode.Active)
                    GameCarrier.Clients.Manager.Service();
                Thread.Sleep(100);
            }

            if (task.Exception != null && task.Exception is AggregateException agg)
                Assert.Fail(agg.InnerException.Message + "\n\n" + agg.InnerException.StackTrace);

            if (task.Exception != null)
                Assert.Fail(task.Exception.Message + "\n\n" + task.Exception.StackTrace);
        }

        private async Task SimpleFlow()
        {
            Console.WriteLine($"Thread before Connect: {Thread.CurrentThread.ManagedThreadId}");

            // Connect
            using var connectAuth = ServiceFactory.Instance.GetConnect<IAuthServiceClient>();
            Result result = await connectAuth.Connect(AuthEndpoint);
            Assert.IsTrue(result.IsOk);
            Console.WriteLine($"Thread after Connect: {Thread.CurrentThread.ManagedThreadId}");

            // Authenticate
            result = await connectAuth.Service.Authenticate(new AuthenticateParameters { Provider = "Test", UserName = "player1" });
            Assert.IsTrue(result.IsOk);
            Console.WriteLine($"Thread after Authenticate: {Thread.CurrentThread.ManagedThreadId}");

            // ListJumpServices
            result = await connectAuth.Service.ListJumpServices(new ListJumpServicesParameters { });
            Assert.IsTrue(result.IsOk);
            Console.WriteLine($"Thread after ListJumpServices: {Thread.CurrentThread.ManagedThreadId}");

            // Disconnect
            result = await connectAuth.Disconnect();
            Assert.IsTrue(result.IsOk);
            Console.WriteLine($"Thread after Disconnect: {Thread.CurrentThread.ManagedThreadId}");
        }

        private async Task LoadFlow()
        {
            Console.WriteLine($"Thread before Connect: {Thread.CurrentThread.ManagedThreadId}");

            // Connect
            using var connectAuth = ServiceFactory.Instance.GetConnect<IAuthServiceClient>();
            Result result = await connectAuth.Connect(AuthEndpoint);
            Assert.IsTrue(result.IsOk);
            Console.WriteLine($"Thread after Auth Connect: {Thread.CurrentThread.ManagedThreadId}");

            string token;
            // Authenticate
            result = await connectAuth.Service.Authenticate(new AuthenticateParameters { Provider = "Test", UserName = "player1" });
            Assert.IsTrue(result.IsOk);
            Console.WriteLine($"Thread after Authenticate: {Thread.CurrentThread.ManagedThreadId}");
            token = ((AuthenticateResult)result).AuthToken;

            using var connectGame1 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            result = await connectGame1.Connect(GameEndpoint);
            Assert.IsTrue(result.IsOk);
            Console.WriteLine($"Thread after Game Connect: {Thread.CurrentThread.ManagedThreadId}");

            result = await connectGame1.Service.Authenticate(
                new AuthenticateParameters { Provider = "Token", Token = token });
            Assert.IsTrue(result.IsOk);
            Console.WriteLine($"Thread after Game Authenticate: {Thread.CurrentThread.ManagedThreadId}");

            // CreateRoom by player1
            result = await connectGame1.Service.CreateRoom(new CreateRoomParameters
            {
                RoomId = "room1",
                RoomProperties = new RoomProperties { IsPrivate = true },
                PlayerId = "player1",
                PlayerProperties = new PlayerProperties { Nickname = "player1" },
            });
            Assert.IsTrue(result.IsOk);
            Console.WriteLine($"Thread after CreateRoom: {Thread.CurrentThread.ManagedThreadId}");

            int count = 0;
            string disconnectReason = null;
            connectGame1.OnDisconnected += reason => disconnectReason = $"Disconnected by server: {reason}";

            var start = DateTime.UtcNow;

            try
            {
                while (DateTime.UtcNow.Subtract(start).TotalMilliseconds <= 1000)
                {
                    connectGame1.Service.Player.UpdateProperties(new PlayerProperties
                    {
                        Nickname = "player1-1"
                    });
                    count++;
                    await Task.Delay(15);
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
    }
}
