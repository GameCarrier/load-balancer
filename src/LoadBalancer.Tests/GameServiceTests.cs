using LoadBalancer.Auth;
using LoadBalancer.Client.Common;
using LoadBalancer.Common;
using LoadBalancer.Game;

namespace LoadBalancer.Tests
{
    [TestClass]
    public class GameServiceTests
    {
        private static readonly Endpoint AuthEndpoint = Endpoint.Parse("wss://127.0.0.1:7700/auth");
        private static readonly Endpoint GameEndpoint = Endpoint.Parse("wss://127.0.0.1:7731/gameNorthAmerica");

        [TestInitialize]
        public void TestInitialize()
        {
            ServiceConnect.InitClientLibraryLogging("GameServiceTests.log");
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

            using var connectGame1 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();

            var result = await connectGame1.Connect(GameEndpoint);
            Assert.IsTrue(result.IsOk);

            result = await connectGame1.Service.Authenticate(
                new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });
            Assert.IsTrue(result.IsOk);

            // CreateRoom by player1
            result = await connectGame1.Service.CreateRoom(new CreateRoomParameters
            {
                RoomId = "room1",
                RoomProperties = new RoomProperties { IsPrivate = true },
                PlayerId = "player1",
                PlayerProperties = new PlayerProperties { Nickname = "player1" },
            });
            Assert.IsTrue(result.IsOk);

            {   // ***************** Assert Creator side
                var CurrentRoom = connectGame1.Service.Room;
                var CurrentPlayer = connectGame1.Service.Player;

                // Assert Room
                Assert.IsNotNull(CurrentRoom);
                Assert.AreEqual("room1", CurrentRoom.RoomId);
                Assert.IsTrue(CurrentRoom.Properties.IsPrivate);

                // Assert Player
                Assert.IsNotNull(CurrentPlayer);
                Assert.AreEqual("player1", CurrentPlayer.PlayerId);
                Assert.IsTrue(CurrentPlayer.IsMyPlayer);

                // Assert Players
                Assert.AreEqual(1, CurrentRoom.Players.Count);
                var player1 = CurrentRoom.Players["player1"];

                Assert.AreEqual(CurrentPlayer, player1);
            }

            // Disconnect
            result = await connectGame1.Disconnect();
            Assert.IsTrue(result.IsOk);

            // Assert all reset
            Assert.IsNull(connectGame1.Service.Room);
            Assert.IsNull(connectGame1.Service.Player);
        }

        [TestMethod]
        public async Task JoinRoom()
        {
            // Arrange Connections
            var resultAuth1 = await Authenticate("player1");
            var resultAuth2 = await Authenticate("player2");

            using var connectGame1 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            using var connectGame2 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();

            // CreateRoom by player1
            await connectGame1.Connect(GameEndpoint);
            await connectGame1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });
            await connectGame1.Service.CreateRoom(new CreateRoomParameters
            {
                RoomId = "room1",
                RoomProperties = new RoomProperties { IsPrivate = true },
                PlayerId = "player1",
                PlayerProperties = new PlayerProperties { Nickname = "player1" },
            });

            // JoinRoom by player2
            await connectGame2.Connect(GameEndpoint);
            await connectGame2.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth2.AuthToken });
            await connectGame2.Service.JoinRoom(new JoinRoomParameters
            {
                RoomId = "room1",
                PlayerId = "player2",
                PlayerProperties = new PlayerProperties { Nickname = "player2" },
            });

            {   // ***************** Assert Creator side
                var CurrentRoom = connectGame1.Service.Room;
                var CurrentPlayer = connectGame1.Service.Player;

                // Assert Player
                Assert.IsNotNull(CurrentPlayer);
                Assert.AreEqual("player1", CurrentPlayer.PlayerId);
                Assert.IsTrue(CurrentPlayer.IsMyPlayer);

                // Assert Players
                Assert.AreEqual(2, CurrentRoom.Players.Count);
                var player1 = CurrentRoom.Players["player1"];
                var player2 = CurrentRoom.Players["player2"];

                Assert.AreEqual(CurrentPlayer, player1);

                Assert.AreEqual("player2", player2.Properties.Nickname);
                Assert.IsFalse(player2.IsMyPlayer);
            }

            {   // ***************** Assert Joiner side
                var CurrentRoom = connectGame2.Service.Room;
                var CurrentPlayer = connectGame2.Service.Player;

                // Assert Room
                Assert.IsNotNull(CurrentRoom);
                Assert.AreEqual("room1", CurrentRoom.RoomId);
                Assert.IsTrue(CurrentRoom.Properties.IsPrivate);

                // Assert Player
                Assert.IsNotNull(CurrentPlayer);
                Assert.AreEqual("player2", CurrentPlayer.PlayerId);
                Assert.IsTrue(CurrentPlayer.IsMyPlayer);

                // Assert Players
                Assert.AreEqual(2, CurrentRoom.Players.Count);
                var player1 = CurrentRoom.Players["player1"];
                var player2 = CurrentRoom.Players["player2"];

                Assert.AreEqual(connectGame2.Service.Player, player2);

                Assert.AreEqual("player1", player1.Properties.Nickname);
                Assert.IsFalse(player1.IsMyPlayer);
            }

            // Disconnect player1
            await connectGame1.Disconnect();
            Assert.IsNull(connectGame1.Service.Room);
            Assert.IsNull(connectGame1.Service.Player);

            await Task.Delay(500);

            {   // ***************** Assert Joiner side
                var CurrentRoom = connectGame2.Service.Room;
                var CurrentPlayer = connectGame2.Service.Player;

                Assert.IsNotNull(CurrentPlayer);
                Assert.AreEqual("player2", CurrentPlayer.PlayerId);

                Assert.AreEqual(1, CurrentRoom.Players.Count);
                Assert.AreEqual(CurrentPlayer, CurrentRoom.Players["player2"]);
            }
        }

        [TestMethod]
        public async Task UpdateRoomProperties()
        {
            // Arrange Connections
            var resultAuth1 = await Authenticate("player1");
            var resultAuth2 = await Authenticate("player2");

            using var connectGame1 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            using var connectGame2 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();

            // CreateRoom by player1
            await connectGame1.Connect(GameEndpoint);
            await connectGame1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });
            await connectGame1.Service.CreateRoom(new CreateRoomParameters
            {
                RoomId = "room1",
                RoomProperties = new RoomProperties { IsPrivate = true },
                PlayerId = "player1",
                PlayerProperties = new PlayerProperties { Nickname = "player1" },
            });

            // JoinRoom by player2
            await connectGame2.Connect(GameEndpoint);
            await connectGame2.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth2.AuthToken });
            await connectGame2.Service.JoinRoom(new JoinRoomParameters
            {
                RoomId = "room1",
                PlayerId = "player2",
                PlayerProperties = new PlayerProperties { Nickname = "player2" },
            });

            Assert.IsTrue(connectGame1.Service.Room.Properties.IsPrivate);
            Assert.IsTrue(connectGame2.Service.Room.Properties.IsPrivate);

            // Change Room properties on Creator side
            connectGame1.Service.Room.UpdateProperties(new RoomProperties { IsPrivate = false });
            await connectGame2.Service.Room.ExpectPropertiesChanged(timeout: 500);

            // Assert Room properties on both sides
            Assert.IsFalse(connectGame1.Service.Room.Properties.IsPrivate);
            Assert.IsFalse(connectGame2.Service.Room.Properties.IsPrivate);

            // Change Room properties on Joiner side
            connectGame2.Service.Room.UpdateProperties(new RoomProperties { IsPrivate = true });
            await connectGame1.Service.Room.ExpectPropertiesChanged(timeout: 500);

            // Assert Room properties on both sides
            Assert.IsTrue(connectGame1.Service.Room.Properties.IsPrivate);
            Assert.IsTrue(connectGame2.Service.Room.Properties.IsPrivate);

            // Change Room properties on Creator side (Change tracking)
            connectGame1.Service.Room.EnableChangeTracking();
            connectGame1.Service.Room.Properties.IsPrivate = false;
            connectGame1.Service.Room.CommitChanges();
            await connectGame2.Service.Room.ExpectPropertiesChanged(timeout: 500);

            // Assert Room properties on both sides
            Assert.IsFalse(connectGame1.Service.Room.Properties.IsPrivate);
            Assert.IsFalse(connectGame2.Service.Room.Properties.IsPrivate);
        }

        [TestMethod]
        public async Task UpdatePlayerProperties()
        {
            // Arrange Connections
            var resultAuth1 = await Authenticate("player1");
            var resultAuth2 = await Authenticate("player2");

            using var connectGame1 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            using var connectGame2 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();

            // CreateRoom by player1
            await connectGame1.Connect(GameEndpoint);
            await connectGame1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });
            await connectGame1.Service.CreateRoom(new CreateRoomParameters
            {
                RoomId = "room1",
                RoomProperties = new RoomProperties { IsPrivate = true },
                PlayerId = "player1",
                PlayerProperties = new PlayerProperties { Nickname = "player1" },
            });

            // JoinRoom by player2
            await connectGame2.Connect(GameEndpoint);
            await connectGame2.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth2.AuthToken });
            await connectGame2.Service.JoinRoom(new JoinRoomParameters
            {
                RoomId = "room1",
                PlayerId = "player2",
                PlayerProperties = new PlayerProperties { Nickname = "player2" },
            });

            // Change Player properties on Creator side
            connectGame1.Service.Player.UpdateProperties(new PlayerProperties
            {
                Nickname = "player1-1",
                Position = new Point3f(1, 2, 3)
            });
            await connectGame2.Service.Room.Players["player1"].ExpectPropertiesChanged(timeout: 500);

            // Assert Player properties on both sides
            Assert.AreEqual("player1-1", connectGame1.Service.Room.Players["player1"].Properties.Nickname);
            Assert.AreEqual("player1-1", connectGame2.Service.Room.Players["player1"].Properties.Nickname);

            Assert.AreEqual(new Point3f(1, 2, 3), connectGame1.Service.Room.Players["player1"].Properties.Position);
            Assert.AreEqual(new Point3f(1, 2, 3), connectGame2.Service.Room.Players["player1"].Properties.Position);

            // Change Player properties on Joiner side
            connectGame2.Service.Player.UpdateProperties(new PlayerProperties { Nickname = "player2-2" });
            await connectGame1.Service.Room.Players["player2"].ExpectPropertiesChanged(timeout: 500);

            // Assert Player properties on both sides
            Assert.AreEqual("player2-2", connectGame1.Service.Room.Players["player2"].Properties.Nickname);
            Assert.AreEqual("player2-2", connectGame2.Service.Room.Players["player2"].Properties.Nickname);

            // Change Player properties on Creator side (Change tracking)
            connectGame1.Service.Player.EnableChangeTracking();
            connectGame1.Service.Player.Properties.Nickname = "player1-1-1";
            connectGame1.Service.Player.Properties.Position = new Point3f(3, 3, 3);
            connectGame1.Service.Player.CommitChanges();
            await connectGame2.Service.Room.Players["player1"].ExpectPropertiesChanged(timeout: 500);

            // Assert Player properties on both sides
            Assert.AreEqual("player1-1-1", connectGame1.Service.Room.Players["player1"].Properties.Nickname);
            Assert.AreEqual("player1-1-1", connectGame2.Service.Room.Players["player1"].Properties.Nickname);

            Assert.AreEqual(new Point3f(3, 3, 3), connectGame1.Service.Room.Players["player1"].Properties.Position);
            Assert.AreEqual(new Point3f(3, 3, 3), connectGame2.Service.Room.Players["player1"].Properties.Position);
        }

        [TestMethod]
        public async Task RoomEvent()
        {
            // Arrange Connections
            var resultAuth1 = await Authenticate("player1");
            var resultAuth2 = await Authenticate("player2");

            using var connectGame1 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            using var connectGame2 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();

            // CreateRoom by player1
            await connectGame1.Connect(GameEndpoint);
            await connectGame1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });
            await connectGame1.Service.CreateRoom(new CreateRoomParameters
            {
                RoomId = "room1",
                RoomProperties = new RoomProperties { IsPrivate = true },
                PlayerId = "player1",
                PlayerProperties = new PlayerProperties { Nickname = "player1" },
            });

            // JoinRoom by player2
            await connectGame2.Connect(GameEndpoint);
            await connectGame2.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth2.AuthToken });
            await connectGame2.Service.JoinRoom(new JoinRoomParameters
            {
                RoomId = "room1",
                PlayerId = "player2",
                PlayerProperties = new PlayerProperties { Nickname = "player2" },
            });

            RoomEvent event1 = null;
            RoomEvent event2 = null;
            connectGame1.Service.Room.OnEventReceived += e => event1 = e;
            connectGame2.Service.Room.OnEventReceived += e => event2 = e;

            // RaiseRoomEvent on Creator side (wrong recipient)
            connectGame1.Service.Room.RaiseRoomEvent(GameMethods.ApplyForce, new KeyValueCollection(), recipientId: "null");
            await connectGame2.Service.Room.ExpectEventReceived(timeout: 500);
            // Assert
            Assert.IsNull(event1);
            Assert.IsNull(event2);

            // RaiseRoomEvent on Creator side (all recipients)
            connectGame1.Service.Room.RaiseRoomEvent(GameMethods.ApplyForce, new KeyValueCollection());
            await connectGame2.Service.Room.ExpectEventReceived(timeout: 500);
            // Assert
            Assert.IsNull(event1);
            Assert.IsNotNull(event2);
        }

        [TestMethod]
        public async Task RoomObject()
        {
            // Arrange Connections
            var resultAuth1 = await Authenticate("player1");
            var resultAuth2 = await Authenticate("player2");

            using var connectGame1 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            using var connectGame2 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();

            // CreateRoom by player1
            await connectGame1.Connect(GameEndpoint);
            await connectGame1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });
            await connectGame1.Service.CreateRoom(new CreateRoomParameters
            {
                RoomId = "room1",
                RoomProperties = new RoomProperties { IsPrivate = true },
                PlayerId = "player1",
                PlayerProperties = new PlayerProperties { Nickname = "player1" },
                RoomObjects = new CreateRoomParameters.Object[]
                {
                    new CreateRoomParameters.Object
                    {
                        ObjectId = "obj0",
                        Tag = "Box",
                        ObjectProperties = new RoomObjectProperties
                        {
                            Position = new Point3f(1, 1, 1),
                            Rotation = new Point3f(0, 0, 0),
                        }
                    }
                }
            });

            // JoinRoom by player2
            await connectGame2.Connect(GameEndpoint);
            await connectGame2.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth2.AuthToken });
            await connectGame2.Service.JoinRoom(new JoinRoomParameters
            {
                RoomId = "room1",
                PlayerId = "player2",
                PlayerProperties = new PlayerProperties { Nickname = "player2" },
            });

            // Assert initially spawned object exists on both sides
            var obj0player1 = connectGame1.Service.Room.Objects["obj0"];
            var obj0player2 = connectGame2.Service.Room.Objects["obj0"];
            Assert.IsNotNull(obj0player1);
            Assert.IsNotNull(obj0player2);

            // Spawn object by Creator
            await connectGame1.Service.SpawnObject(new SpawnObjectParameters
            {
                ObjectId = "obj1",
                Tag = "Box",
                ObjectProperties = new RoomObjectProperties
                {
                    Position = new Point3f(1, 1, 1),
                    Rotation = new Point3f(0, 0, 0),
                }
            });

            // Assert spawned object exists on both sides
            var obj1player1 = connectGame1.Service.Room.Objects["obj1"];
            Assert.IsNotNull(obj1player1);
            Assert.AreEqual(new Point3f(1, 1, 1), obj1player1.Properties.Position);
            Assert.AreEqual(new Point3f(0, 0, 0), obj1player1.Properties.Rotation);

            var obj1player2 = connectGame2.Service.Room.Objects["obj1"];
            Assert.IsNotNull(obj1player2);
            Assert.AreEqual(new Point3f(1, 1, 1), obj1player2.Properties.Position);
            Assert.AreEqual(new Point3f(0, 0, 0), obj1player2.Properties.Rotation);

            // Update object properties on Creator side
            obj1player1.UpdateProperties(new RoomObjectProperties
            {
                Position = new Point3f(1, 2, 3)
            });
            await obj1player2.ExpectPropertiesChanged(timeout: 500);

            // Assert object properties changed on both sides
            Assert.AreEqual(new Point3f(1, 2, 3), obj1player1.Properties.Position);
            Assert.AreEqual(new Point3f(1, 2, 3), obj1player2.Properties.Position);

            // Update object properties on Joiner side
            obj1player2.UpdateProperties(new RoomObjectProperties
            {
                Position = new Point3f(3, 2, 1)
            });
            await obj1player1.ExpectPropertiesChanged(timeout: 500);

            // Assert object properties changed on both sides
            Assert.AreEqual(new Point3f(3, 2, 1), obj1player1.Properties.Position);
            Assert.AreEqual(new Point3f(3, 2, 1), obj1player2.Properties.Position);

            // Destroy object on Creator side
            obj1player1.Destroy();
            await connectGame2.Service.Room.ExpectObjectDestroyed(timeout: 500);

            // Assert object destroyed on both sides
            obj1player1 = connectGame1.Service.Room.Objects["obj1"];
            obj1player2 = connectGame2.Service.Room.Objects["obj1"];
            Assert.IsNull(obj1player1);
            Assert.IsNull(obj1player2);

            // Destroy object on Joiner side
            obj0player2.Destroy();
            await connectGame1.Service.Room.ExpectObjectDestroyed(timeout: 500);

            // Assert object destroyed on both sides
            obj0player1 = connectGame1.Service.Room.Objects["obj0"];
            obj0player2 = connectGame2.Service.Room.Objects["obj0"];
            Assert.IsNull(obj0player1);
            Assert.IsNull(obj0player2);
        }

        [TestMethod]
        public async Task HostElection()
        {
            // Arrange Connections
            var resultAuth1 = await Authenticate("player1");
            var resultAuth2 = await Authenticate("player2");

            using var connectGame1 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
            using var connectGame2 = ServiceFactory.Instance.GetConnect<IGameServiceClient>();

            // CreateRoom by player1
            await connectGame1.Connect(GameEndpoint);
            await connectGame1.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth1.AuthToken });
            await connectGame1.Service.CreateRoom(new CreateRoomParameters
            {
                RoomId = "room1",
                RoomProperties = new RoomProperties { IsPrivate = true },
                PlayerId = "player1",
                PlayerProperties = new PlayerProperties { Nickname = "player1" },
            });

            Assert.IsTrue(connectGame1.Service.Player.Properties.IsHost);

            // JoinRoom by player2
            await connectGame2.Connect(GameEndpoint);
            await connectGame2.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = resultAuth2.AuthToken });
            await connectGame2.Service.JoinRoom(new JoinRoomParameters
            {
                RoomId = "room1",
                PlayerId = "player2",
                PlayerProperties = new PlayerProperties { Nickname = "player2" },
            });

            {   // ***************** Assert Creator side
                var CurrentPlayer = connectGame1.Service.Player;
                Assert.IsTrue(CurrentPlayer.Properties.IsHost);
            }

            {   // ***************** Assert Joiner side
                var CurrentPlayer = connectGame2.Service.Player;
                Assert.IsFalse(CurrentPlayer.Properties.IsHost);
            }

            // Disconnect player1
            await connectGame1.Disconnect();
            Assert.IsNull(connectGame1.Service.Room);
            Assert.IsNull(connectGame1.Service.Player);

            await Task.Delay(500);

            {   // ***************** Assert Joiner side
                var CurrentPlayer = connectGame2.Service.Player;
                Assert.IsTrue(CurrentPlayer.Properties.IsHost);
            }
        }
    }
}
