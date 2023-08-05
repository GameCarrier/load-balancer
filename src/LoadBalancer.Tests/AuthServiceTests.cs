using LoadBalancer.Auth;
using LoadBalancer.Client.Common;
using LoadBalancer.Common;

namespace LoadBalancer.Tests
{
    [TestClass]
    public class AuthServiceTests
    {
        private static readonly Endpoint AuthEndpoint = Endpoint.Parse("wss://127.0.0.1:7700/auth");

        [TestInitialize]
        public void TestInitialize()
        {
            ServiceConnect.InitClientLibraryLogging("AuthServiceTests.log");
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
        public async Task Connect_AuthService()
        {
            using var connectAuth = ServiceFactory.Instance.GetConnect<IAuthServiceClient>();
            var result = await connectAuth.Connect(AuthEndpoint);
            Assert.IsTrue(result.IsOk);
            Assert.IsTrue(connectAuth.IsConnected);

            result = await connectAuth.Disconnect();
            Assert.IsFalse(connectAuth.IsConnected);
        }

        [TestMethod]
        public async Task Connect_AuthService_Ping()
        {
            using var connectAuth = ServiceFactory.Instance.GetConnect<IAuthServiceClient>();
            await connectAuth.Connect(AuthEndpoint);
            Assert.IsTrue(connectAuth.IsConnected);

            var result = await connectAuth.Ping();
            Console.WriteLine($"Ping: {result.PingMiliseconds}");
            Assert.IsTrue(result.IsOk);
        }

        [TestMethod]
        public async Task Connect_AuthService_Authenticate()
        {
            using var connectAuth = ServiceFactory.Instance.GetConnect<IAuthServiceClient>();
            await connectAuth.Connect(AuthEndpoint);
            Assert.IsTrue(connectAuth.IsConnected);

            var result = await connectAuth.Service.Authenticate(new AuthenticateParameters { Provider = "Test", UserName = "player1" });
            Console.WriteLine(result);
            Assert.IsTrue(result.IsOk);
            Console.WriteLine(result.AuthToken);
        }

        [TestMethod]
        public async Task Connect_AuthService_Authenticate_Error_UsernameMissed()
        {
            using var connectAuth = ServiceFactory.Instance.GetConnect<IAuthServiceClient>();
            await connectAuth.Connect(AuthEndpoint);
            Assert.IsTrue(connectAuth.IsConnected);

            var result = await connectAuth.Service.Authenticate(new AuthenticateParameters { Provider = "Test" });
            Console.WriteLine(result);
            Assert.IsFalse(result.IsOk);
        }

        [TestMethod]
        public async Task Connect_AuthService_Authenticate_ListJumpServices()
        {
            using var connectAuth = ServiceFactory.Instance.GetConnect<IAuthServiceClient>();
            await connectAuth.Connect(AuthEndpoint);
            Assert.IsTrue(connectAuth.IsConnected);

            {   // Authenticate
                var result = await connectAuth.Service.Authenticate(new AuthenticateParameters { Provider = "Test", UserName = "player1" });
                Console.WriteLine(result);
                Assert.IsTrue(result.IsOk);
            }

            {   // ListJumpServices
                var result = await connectAuth.Service.ListJumpServices(new ListJumpServicesParameters { });
                Console.WriteLine($"{result.Status} ({result.Message}): Count: {result.Endpoints.Length}");
                foreach (var endpoint in result.Endpoints)
                    Console.WriteLine($"* {endpoint}");
                Assert.AreEqual(2, result.Endpoints.Length);
            }

            {   // ListJumpServices
                var result = await connectAuth.Service.ListJumpServices(new ListJumpServicesParameters { Region = "NorthAmerica" });
                Console.WriteLine($"{result.Status} ({result.Message}): Count: {result.Endpoints.Length}");
                foreach (var endpoint in result.Endpoints)
                    Console.WriteLine($"* {endpoint}");
                Assert.AreEqual(1, result.Endpoints.Length);
            }
        }
    }
}
