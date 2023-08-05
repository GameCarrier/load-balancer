using LoadBalancer.Auth;
using LoadBalancer.Client.Common;
using LoadBalancer.Common;
using LoadBalancer.Game;
using LoadBalancer.Jump;

namespace LoadBalancer.Tests
{
    [TestClass]
    public class IntegrationTests
    {
        private static readonly Endpoint AuthEndpoint = Endpoint.Parse("wss://127.0.0.1:7700/auth");
        // private static readonly Endpoint AuthEndpoint = Endpoint.Parse("wss://127.0.0.1:7701/auth1");
        // private static readonly Endpoint AuthEndpoint = Endpoint.Parse("wss://127.0.0.1:7702/auth2");

        [TestInitialize]
        public void TestInitialize()
        {
            ServiceConnect.InitClientLibraryLogging("IntegrationTests.log", GameCarrier.Common.LogLevel.LLL_VERBOSE);
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
        public async Task CreateRoom_JoinRoom()
        {
            // Connect to Auth ---------------------------- Player1 ----------------------------
            using var connectAuth1 = ServiceFactory.Instance.GetConnect<IAuthServiceClient>();
            await connectAuth1.Connect(AuthEndpoint);
            var resultAuth1 = await connectAuth1.Service.Authenticate(new AuthenticateParameters { Provider = "Test", UserName = "player1" });

            // ListJumpServices
            var resultListJumpServices = await connectAuth1.Service.ListJumpServices(
                new ListJumpServicesParameters { Region = "NorthAmerica" });
            Assert.AreEqual("wss://127.0.0.1:7711/jumpNorthAmerica", resultListJumpServices.Endpoints[0].ToString());

            await connectAuth1.Disconnect();

            var closestJumpServiceResult = await connectAuth1.Service.SelectClosestService(resultListJumpServices.Endpoints);
            Assert.IsTrue(closestJumpServiceResult.IsOk);

            // Connect to Jump
            using var connectJump1 = ServiceFactory.Instance.GetConnect<IJumpServiceClient>();
            await connectJump1.Connect(closestJumpServiceResult.ServiceEndpoint);
            await connectJump1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });

            // Find Room
            var resultFindRoom = await connectJump1.Service.FindRoom(
                new FindRoomParameters { RoomProperties = new RoomProperties { IsPrivate = false } });
            Assert.AreEqual(JumpErrors.Error_RoomNotFound, resultFindRoom.Status);

            // Find Server
            var resultFindServer = await connectJump1.Service.FindServer(
                new FindServerParameters { RoomProperties = new RoomProperties { IsPrivate = false } });

            Assert.AreEqual(JumpErrors.Ok, resultFindServer.Status.As<JumpErrors>());

            Assert.AreEqual("wss://127.0.0.1:7731/gameNorthAmerica", resultFindServer.Room.ServiceEndpoint.ToString());

            await connectJump1.Disconnect();

            // Connect to Game
            using var connectGame1 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            await connectGame1.Connect(resultFindServer.Room.ServiceEndpoint);
            await connectGame1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });

            // Create Room
            await connectGame1.Service.CreateRoom(new CreateRoomParameters
            {
                RoomId = resultFindServer.Room.RoomId,
                RoomProperties = resultFindServer.Room.RoomProperties,
                PlayerId = "player1",
                PlayerProperties = new PlayerProperties { Nickname = "player1" },
            });

            await Task.Delay(500);

            // Connect to Auth ---------------------------- Player2 ----------------------------
            using var connectAuth2 = ServiceFactory.Instance.GetConnect<IAuthServiceClient>();
            await connectAuth2.Connect(AuthEndpoint);
            var resultAuth2 = await connectAuth2.Service.Authenticate(new AuthenticateParameters { Provider = "Test", UserName = "player2" });

            // ListJumpServices
            resultListJumpServices = await connectAuth2.Service.ListJumpServices(
                new ListJumpServicesParameters { Region = "NorthAmerica" });

            await connectAuth2.Disconnect();

            closestJumpServiceResult = await connectAuth2.Service.SelectClosestService(resultListJumpServices.Endpoints);
            Assert.IsTrue(closestJumpServiceResult.IsOk);

            // Connect to Jump
            using var connectJump2 = ServiceFactory.Instance.GetConnect<IJumpServiceClient>();
            await connectJump2.Connect(closestJumpServiceResult.ServiceEndpoint);
            await connectJump2.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth2.AuthToken });

            // Find Room
            resultFindRoom = await connectJump2.Service.FindRoom(
                new FindRoomParameters { RoomProperties = new RoomProperties { IsPrivate = false } });
            Assert.AreEqual(1, resultFindRoom.Rooms.Length);

            // Connect to Game
            using var connectGame2 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            await connectGame2.Connect(resultFindRoom.Rooms[0].ServiceEndpoint);
            await connectGame2.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth2.AuthToken });

            // Join Room
            await connectGame2.Service.JoinRoom(new JoinRoomParameters
            {
                RoomId = resultFindRoom.Rooms[0].RoomId,
                PlayerId = "player2",
                PlayerProperties = new PlayerProperties { Nickname = "player2" },
            });

            await Task.Delay(500);

            // Assert FindRoom by PlayerIds (Creator and Joiner found)
            resultFindRoom = await connectJump2.Service.FindRoom(
                new FindRoomParameters { PlayerIds = new[] { "player1" } });
            Assert.AreEqual(1, resultFindRoom.Rooms.Length);

            resultFindRoom = await connectJump2.Service.FindRoom(
                new FindRoomParameters { PlayerIds = new[] { "player2" } });
            Assert.AreEqual(1, resultFindRoom.Rooms.Length);

            // Disconnect Room Creator
            await connectGame1.Disconnect();

            await Task.Delay(500);

            // Assert FindRoom by PlayerIds (only Joiner left)
            resultFindRoom = await connectJump2.Service.FindRoom(
                new FindRoomParameters { PlayerIds = new[] { "player1" } });
            Assert.AreEqual(JumpErrors.Error_RoomNotFound, resultFindRoom.Status);

            resultFindRoom = await connectJump2.Service.FindRoom(
                new FindRoomParameters { PlayerIds = new[] { "player2" } });
            Assert.AreEqual(1, resultFindRoom.Rooms.Length);

            // Disconnect Room Joiner
            await connectGame2.Disconnect();

            await Task.Delay(500);

            // Assert FindRoom by PlayerIds (not found)
            resultFindRoom = await connectJump2.Service.FindRoom(
                new FindRoomParameters { PlayerIds = new[] { "player2" } });
            Assert.AreEqual(JumpErrors.Error_RoomNotFound, resultFindRoom.Status);
        }
    }
}
