using LoadBalancer.Auth;
using LoadBalancer.Client.Common;
using LoadBalancer.Common;
using LoadBalancer.Game;
using LoadBalancer.Jump;

namespace LoadBalancer.Tests
{
    [TestClass]
    public class JumpServiceTests
    {
        private static readonly Endpoint AuthEndpoint = Endpoint.Parse("wss://127.0.0.1:7700/auth");
        private static readonly Endpoint JumpEndpoint = Endpoint.Parse("wss://127.0.0.1:7711/jumpNorthAmerica");
        private static readonly Endpoint GameEndpoint = Endpoint.Parse("wss://127.0.0.1:7731/gameNorthAmerica");

        [TestInitialize]
        public void TestInitialize()
        {
            ServiceConnect.InitClientLibraryLogging("JumpServiceTests.log");
            ServiceConnect.InitClientLibraryMode(GameCarrier.Clients.GcClientMode.Active);

            Client.Bootstrapper.RegisterTypes();
            Client.Bootstrapper.ConfigureServiceFactory(ServiceFactory.Instance);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ServiceConnect.CleanupClientLibraryMode();
        }

        private static async Task<AuthenticateResult> Authenticate(string userName)
        {
            using var connectAuth = ServiceFactory.Instance.GetConnect<IAuthServiceClient>();
            await connectAuth.Connect(AuthEndpoint);
            var result = await connectAuth.Service.Authenticate(
                new AuthenticateParameters { Provider = "Test", UserName = userName });
            return result;
        }

        [TestMethod]
        public async Task CreateRoom()
        {
            // Arrange Connections
            var resultAuth1 = await Authenticate("player1");

            using var connectJump1 = ServiceFactory.Instance.GetConnect<IJumpServiceClient>();
            await connectJump1.Connect(JumpEndpoint);
            await connectJump1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });

            // Assert no Rooms
            var resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters { });
            Assert.AreEqual(JumpErrors.Error_RoomNotFound, resultFindRoom.Status);

            using var connectGame1 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            await connectGame1.Connect(GameEndpoint);
            await connectGame1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });

            // CreateRoom
            await connectGame1.Service.CreateRoom(new CreateRoomParameters
            {
                RoomId = "room1",
                RoomProperties = new RoomProperties { IsPrivate = true },
                PlayerId = "player1",
                PlayerProperties = new PlayerProperties { Nickname = "player1" },
            });

            await Task.Delay(500);

            // Assert Room can be found
            resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters { });
            Assert.AreEqual(1, resultFindRoom.Rooms.Length);

            resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters { PlayerIds = new[] { "player1" } });
            Assert.AreEqual(1, resultFindRoom.Rooms.Length);

            resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters { PlayerIds = new[] { "player2" } });
            Assert.AreEqual(JumpErrors.Error_RoomNotFound, resultFindRoom.Status);

            // Disconnect
            await connectGame1.Disconnect();

            // Assert no Rooms
            await Task.Delay(500);
            resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters { });
            Assert.AreEqual(JumpErrors.Error_RoomNotFound, resultFindRoom.Status);
        }

        [TestMethod]
        public async Task JoinRoom()
        {
            // Arrange Connections
            var resultAuth1 = await Authenticate("player1");
            var resultAuth2 = await Authenticate("player2");

            using var connectJump1 = ServiceFactory.Instance.GetConnect<IJumpServiceClient>();
            await connectJump1.Connect(JumpEndpoint);
            await connectJump1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });

            using var connectGame1 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            await connectGame1.Connect(GameEndpoint);
            await connectGame1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });

            using var connectGame2 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            await connectGame2.Connect(GameEndpoint);
            await connectGame2.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth2.AuthToken });

            // CreateRoom
            await connectGame1.Service.CreateRoom(new CreateRoomParameters
            {
                RoomId = "room1",
                RoomProperties = new RoomProperties { IsPrivate = true },
                PlayerId = "player1",
                PlayerProperties = new PlayerProperties { Nickname = "player1" },
            });

            // Assert room can be found
            var resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters { });

            // Join Room
            await connectGame2.Service.JoinRoom(new JoinRoomParameters
            {
                RoomId = resultFindRoom.Rooms[0].RoomId,
                PlayerId = "player2",
                PlayerProperties = new PlayerProperties { Nickname = "player2" },
            });

            await Task.Delay(500);

            // Assert room can be found by PlayerIds
            resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters { PlayerIds = new[] { "player1" } });
            Assert.AreEqual(1, resultFindRoom.Rooms.Length);

            resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters { PlayerIds = new[] { "player2" } });
            Assert.AreEqual(1, resultFindRoom.Rooms.Length);
        }

        [TestMethod]
        public async Task UpdateRoomProperties()
        {
            // Arrange Connections
            var resultAuth1 = await Authenticate("player1");

            using var connectJump1 = ServiceFactory.Instance.GetConnect<IJumpServiceClient>();
            await connectJump1.Connect(JumpEndpoint);
            await connectJump1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });

            using var connectGame1 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            await connectGame1.Connect(GameEndpoint);
            await connectGame1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });

            // CreateRoom
            await connectGame1.Service.CreateRoom(new CreateRoomParameters
            {
                RoomId = "room1",
                RoomProperties = new RoomProperties { IsPrivate = true },
                PlayerId = "player1",
                PlayerProperties = new PlayerProperties { Nickname = "player1" },
            });

            // Assert room can be found
            var resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters { });
            Assert.IsTrue(resultFindRoom.Rooms[0].RoomProperties.GetValue<bool>(RoomKeys.IsPrivate));

            // Assert room can be found by IsPrivate flag
            resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters
            {
                RoomProperties = new RoomProperties { IsPrivate = true }
            });
            Assert.IsTrue(resultFindRoom.IsOk);

            resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters
            {
                RoomProperties = new RoomProperties { IsPrivate = false }
            });
            Assert.IsFalse(resultFindRoom.IsOk);

            // UpdateProperties
            connectGame1.Service.Room.UpdateProperties(new RoomProperties { IsPrivate = false });
            await Task.Delay(500);

            // Assert room can be found
            resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters { });
            Assert.IsFalse(resultFindRoom.Rooms[0].RoomProperties.GetValue<bool>(RoomKeys.IsPrivate));

            // Assert room can be found by IsPrivate flag
            resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters
            {
                RoomProperties = new RoomProperties { IsPrivate = true }
            });
            Assert.IsFalse(resultFindRoom.IsOk);

            resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters
            {
                RoomProperties = new RoomProperties { IsPrivate = false }
            });
            Assert.IsTrue(resultFindRoom.IsOk);
        }

#if DEBUG
        [TestMethod]
        public async Task RestoreJumpServiceConnect()
        {
            // Arrange Connections
            var resultAuth1 = await Authenticate("player1");
            var resultAuth2 = await Authenticate("player2");

            using var connectJump1 = ServiceFactory.Instance.GetConnect<IJumpServiceClient>();
            await connectJump1.Connect(JumpEndpoint);
            await connectJump1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });

            using var connectGame1 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            await connectGame1.Connect(GameEndpoint);
            await connectGame1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });

            using var connectGame2 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            await connectGame2.Connect(GameEndpoint);
            await connectGame2.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth2.AuthToken });

            connectGame1.Service.OnJumpServiceConnectEnabled();
            await Task.Delay(500);

            // CreateRoom
            await connectGame1.Service.CreateRoom(new CreateRoomParameters
            {
                RoomId = "room1",
                RoomProperties = new RoomProperties { IsPrivate = true },
                PlayerId = "player1",
                PlayerProperties = new PlayerProperties { Nickname = "player1" },
            });

            await Task.Delay(500);

            // Assert Room can be found
            var resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters { });
            Assert.AreEqual(1, resultFindRoom.Rooms.Length);

            // Disable connect to JumpService
            connectGame1.Service.OnJumpServiceConnectDisabled();
            await Task.Delay(500);

            // Assert no Rooms on Jump
            resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters { });
            Assert.AreEqual(JumpErrors.Error_RoomNotFound, resultFindRoom.Status);

            // JoinRoom
            await connectGame2.Service.JoinRoom(new JoinRoomParameters
            {
                RoomId = "room1",
                PlayerId = "player2",
                PlayerProperties = new PlayerProperties { Nickname = "player2" },
            });

            await Task.Delay(500);

            // Enable connect to JumpService
            connectGame1.Service.OnJumpServiceConnectEnabled();
            await Task.Delay(500);

            // Assert Room can be found
            resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters { PlayerIds = new[] { "player1" } });
            Assert.AreEqual(1, resultFindRoom.Rooms.Length);

            resultFindRoom = await connectJump1.Service.FindRoom(new FindRoomParameters { PlayerIds = new[] { "player2" } });
            Assert.AreEqual(1, resultFindRoom.Rooms.Length);
        }
#endif
    }
}
