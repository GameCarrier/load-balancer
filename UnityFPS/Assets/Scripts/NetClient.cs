using Cinemachine;
using LoadBalancer;
using LoadBalancer.Auth;
using LoadBalancer.Client.Common;
using LoadBalancer.Common;
using LoadBalancer.Game;
using LoadBalancer.Jump;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetClient : MonoBehaviour
{
    public static NetClient Instance { get; private set; }

    [Header("Connect Properties")]
    public string[] AuthEndpoints;  // = "wss://127.0.0.1:7700/auth";

    public string UserName;
    public string Password;

    public string Region;
    public string TitleId;
    public string Version;

    [Header("Room Properties")]
    public string SceneName;
    public string RoomId;

    public Transform[] SpawnPoints;
    public GameObject PlayerPrefab;

    [Serializable]
    public class ObjectPrefab
    {
        public string tag;
        public GameObject prefab;
    }

    public ObjectPrefab[] ObjectPrefabs;

    private KeyValueCollection CreateRoomProperties() => new RoomProperties
    { 
        SceneName = SceneName,
    };
    
    private KeyValueCollection CreatePlayerProperties(int spawnIndex) => new PlayerProperties
    {
        Nickname = UserName,
        Position = SpawnPoints[spawnIndex].position.ToPoint(),
        Rotation = SpawnPoints[spawnIndex].rotation.ToPointEulerAngles(),
    };

    public string authToken { get; private set; }
    public Endpoint jumpEndpoint { get; private set; }
    public RoomLocator roomLocator { get; private set; }
    public IServiceConnect<IGameServiceClient> ConnectGame { get; private set; }

    public bool IsConnecting { get; private set; }
    public bool IsConnected => ConnectGame != null && ConnectGame.Service.Room != null;
    public IClientRoom CurrentRoom => ConnectGame != null ? ConnectGame.Service.Room : null;
    public IClientPlayer CurrentPlayer => ConnectGame != null ? ConnectGame.Service.Player : null;

    [Header("Net Settings")]
    public int frequency = 20;
    public InterpolationMode InterpolationMode;
    public bool RoomObjectsEnabled;
    public bool RoomObjectsSyncVelocity;
    
    public void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // int i = 1;
        if (HotReloader.IsHotReload)
        {
            Instance = this;

            foreach (var character in GameObject.FindGameObjectsWithTag("Player"))
                Destroy(character);
            
            // Logout();
        }

        // Configure
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string logFilePath = System.IO.Path.Combine(appData, "AppData\\Local\\Temp\\Unity\\Editor\\UnityFPS.log");

        ServiceConnect.InitClientLibraryLogging(logFilePath);
        ServiceConnect.InitClientLibraryMode(GameCarrier.Clients.GcClientMode.Passive);

        LoadBalancer.Client.Bootstrapper.RegisterTypes();
        LoadBalancer.Client.Bootstrapper.ConfigureServiceFactory(ServiceFactory.Instance);

        LogInformation($"Application Started. Frequency: {frequency}, InterpolationMode: {InterpolationMode}");
    }

    void Update()
    {
        GameCarrier.Clients.Manager.Service();
    }

    void OnApplicationQuit()
    {
        _ = DisconnectServer();
    }

    void OnDisable()
    {
        _ = DisconnectServer();
        ServiceConnect.CleanupClientLibraryMode();
    }

    void OnDestroy()
    {
        Instance = null;
    }

    public void Logout()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #region Connect

    public async Task PerformConnection()
    {
        ClearProgress();
        IsConnecting = true;

        try
        {
            // Connect Auth
            Result result = null;

            foreach (var endpoint in AuthEndpoints)
            {
                if (string.IsNullOrEmpty(endpoint)) 
                    continue;

                result = await ConnectAuthService(endpoint);
                if (result.IsOk) 
                    break;
                else 
                    await Task.Delay(1);
            }

            if (!result.IsOk)
            {
                LogError(result);
                return;
            }

            result = await ConnectJumpService();
            if (!result.IsOk)
            {
                LogError(result);
                return;
            }

            result = await ConnectGameService();
            if (!result.IsOk)
            {
                LogError(result);
                return;
            }

            if (!roomLocator.IsExistingRoom)
                LogInformation($"Created room {roomLocator.RoomId} on {roomLocator.ServiceEndpoint}");
            else
                LogInformation($"Joined room {roomLocator.RoomId} on {roomLocator.ServiceEndpoint}");
        }
        catch (Exception ex)
        {
            LogException(ex);
        }
        finally
        {
            if (CurrentRoom == null)
                await DisconnectServer();

            IsConnecting = false; 
        }
    }

    public async Task DisconnectServer()
    {
        LogProgress("DisconnectServer");
        if (ConnectGame != null)
        {
            ConnectGame.OnDisconnected -= ConnectGame_OnDisconnected;
            await ConnectGame.Disconnect();
            ConnectGame = null;
            LogInformation($"Disconnected");
        }
        LogProgress("Ok", 90);

        LogProgress("Destroy Characters");
        foreach (var character in GameObject.FindGameObjectsWithTag("Player"))
            Destroy(character);
        LogProgress("Ok", 95);

        LogProgress("Reset Room Objects");
        foreach (var component in SceneRoomObjects)
            component.ClearState();
        SceneRoomObjects.Clear();
        LogProgress("Ok", 100);
    }

    private async Task<Result> ConnectAuthService(string endpoint)
    {
        authToken = null;
        jumpEndpoint = null;
        var authEndpoint = Endpoint.Parse(endpoint);
        using (var connectAuth = ServiceFactory.Instance.GetConnect<IAuthServiceClient>())
        {
            LogProgress($"Connect Auth {authEndpoint}"); 
            await Task.Delay(1);
            var resultConnect = await connectAuth.Connect(authEndpoint);
            LogProgress(resultConnect.StatusName, 5);
            if (!resultConnect.IsOk)
                return resultConnect;

            LogProgress($"Authenticate Provider: Test, UserName: {UserName}");
            await Task.Delay(1);
            var resultAuth = await connectAuth.Service.Authenticate(new AuthenticateParameters 
            { 
                Provider = "Test", 
                UserName = UserName, 
                Password = Password,
            });
            LogProgress(resultAuth.StatusName, 10);
            if (!resultAuth.IsOk)
                return resultAuth;

            authToken = resultAuth.AuthToken;

            LogProgress($"ListJumpServices Region: {Region}, TitleId: {TitleId}, Version: {Version}");
            await Task.Delay(1);
            var resultListJump = await connectAuth.Service.ListJumpServices(new ListJumpServicesParameters 
            {
                Region = Region,
                TitleId = TitleId,
                Version = Version,
            });
            LogProgress(resultListJump.StatusName, 20);
            if (!resultListJump.IsOk)
                return resultListJump;

            LogProgress($"SelectClosestService of {resultListJump.Endpoints.Length} endpoints");
            await Task.Delay(1);
            var resultSelectClosest = await connectAuth.Service.SelectClosestService(resultListJump.Endpoints);
            LogProgress(resultSelectClosest.StatusName, 30);
            if (!resultSelectClosest.IsOk)
                return resultSelectClosest;

            jumpEndpoint = resultSelectClosest.ServiceEndpoint;
            return Result.Ok();
        }
    }

    private async Task<Result> ConnectJumpService()
    {
        using (var connectJump = ServiceFactory.Instance.GetConnect<IJumpServiceClient>())
        {
            LogProgress($"Connect Jump {jumpEndpoint}");
            await Task.Delay(1);
            var resultConnect = await connectJump.Connect(jumpEndpoint);
            LogProgress(resultConnect.StatusName, 35);
            if (!resultConnect.IsOk)
                return resultConnect;

            LogProgress($"Authenticate Provider: Token");
            await Task.Delay(1);
            var resultAuth = await connectJump.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = authToken });
            LogProgress(resultAuth.StatusName, 40);
            if (!resultAuth.IsOk)
                return resultAuth;

            LogProgress($"FindRoom: SceneName: {SceneName}, RoomId: {RoomId}");
            await Task.Delay(1);
            var resultFindRoom = await connectJump.Service.FindRoom(new FindRoomParameters
            {
                TitleId = TitleId,
                Version = Version,
                RoomId = RoomId,
                RoomProperties = CreateRoomProperties(),
            });
            LogProgress(resultFindRoom.StatusName, 45);

            if (resultFindRoom.IsOk)
            {
                roomLocator = resultFindRoom.Rooms[0];
            }
            else
            if (resultFindRoom.Status.As<GameErrors>() == GameErrors.Error_RoomNotFound)
            {
                LogProgress($"FindServer: TitleId: {TitleId}, Version: {Version}, SceneName: {SceneName}");
                await Task.Delay(1);
                var resultFindServer = await connectJump.Service.FindServer(new FindServerParameters
                {
                    TitleId = TitleId,
                    Version = Version,
                    RoomProperties = CreateRoomProperties(),
                });

                LogProgress(resultFindServer.StatusName, 50);
                if (!resultFindServer.IsOk)
                    return resultFindServer;

                roomLocator = resultFindServer.Room;
            }
            else
            {
                return resultFindRoom;
            }

            return Result.Ok();
        }
    }

    private bool IsInitializingRoom;
    private bool IsJoiningRoom;
    private async Task<Result> ConnectGameService()
    {
        // TODO: dispose connectGame if not NULL

        LogProgress($"Connect Game {roomLocator.ServiceEndpoint}");
        await Task.Delay(1);
        ConnectGame = ServiceFactory.Instance.GetConnect<IGameServiceClient>();
        var resultConnect = await ConnectGame.Connect(roomLocator.ServiceEndpoint);
        LogProgress(resultConnect.StatusName, 55);
        if (!resultConnect.IsOk)
            return resultConnect;

        LogProgress($"Authenticate Provider: Token");
        await Task.Delay(1);
        var resultAuth = await ConnectGame.Service.Authenticate(new AuthenticateParameters { Provider = "Token", Token = authToken });
        LogProgress(resultAuth.StatusName, 60);
        if (!resultAuth.IsOk)
            return resultAuth;

        IsInitializingRoom = false;
        IsJoiningRoom = false;

        LogProgress($"Load Scene {SceneName}");
        await Task.Delay(1);
        if (SceneManager.GetActiveScene().name != SceneName || !roomLocator.IsExistingRoom)
            await Utilities.LoadSceneAsync("Playground");
        LogProgress("Ok", 70);

        string playerId = Guid.NewGuid().ToString();
        if (!roomLocator.IsExistingRoom)
        {
            string roomId = !string.IsNullOrWhiteSpace(RoomId) ? RoomId : roomLocator.RoomId;
            LogProgress($"Create Room RoomId: {roomId}");
            await Task.Delay(1);
            var resultCreateRoom = await ConnectGame.Service.CreateRoom(new CreateRoomParameters
            {
                RoomId = roomId,
                RoomProperties = CreateRoomProperties(),
                PlayerId = playerId,
                PlayerProperties = CreatePlayerProperties(new System.Random().Next(SpawnPoints.Length)),
            });
            LogProgress(resultCreateRoom.StatusName, 80);
            if (!resultCreateRoom.IsOk)
                return resultCreateRoom;
        }
        else
        {
            string roomId = roomLocator.RoomId;
            LogProgress($"Join Room RoomId: {roomId}");
            await Task.Delay(1);
            IsJoiningRoom = true;
            var resultJoinRoom = await ConnectGame.Service.JoinRoom(new JoinRoomParameters
            {
                RoomId = roomId,
                PlayerId = playerId,
                PlayerProperties = CreatePlayerProperties(new System.Random().Next(SpawnPoints.Length)),
            });
            LogProgress(resultJoinRoom.StatusName, 80);
            if (!resultJoinRoom.IsOk)
                return resultJoinRoom;

            foreach (var roomPlayer in resultJoinRoom.RoomPlayers)
            {
                var player = CurrentRoom.Players[roomPlayer.PlayerId];
                CreateOtherPlayer(player);
            }
        }

        IsInitializingRoom = true;

        LogProgress($"Init Room Objects");
        await Task.Delay(10); // let RoomObjects start their registration

        while (SceneRoomObjects.Any(o => !o.isConfigured))
            await Task.Delay(100);

        IsJoiningRoom = false;
        IsInitializingRoom = false;

        foreach (var obj in CurrentRoom.Objects.Except(SceneRoomObjects.Select(o => o.RoomObject)))
            SpawnObject(obj);

        LogProgress("Ok", 90);

        LogProgress($"Create Main Player");
        await Task.Delay(1);
        CreateMainPlayer(CurrentPlayer);

        ConnectGame.OnDisconnected += ConnectGame_OnDisconnected;

        CurrentRoom.OnEventReceived += CurrentRoom_OnEventReceived;
        CurrentRoom.Objects.OnSpawned += SpawnObject;
        CurrentRoom.Players.OnJoin += CreateOtherPlayer;

        LogProgress($"Ok", 100);

        return Result.Ok();
    }

    private void CurrentRoom_OnEventReceived(RoomEvent evt)
    {
        if (evt.Name == GameMethods.ApplyForce)
        {
            var args = new RoomEventArguments(evt.Parameters);
            var component = FindRoomObjectComponent(args.ObjectId);
            if (component != null) 
                component.ApplyForceActive(args);
        }
    }

    private void ConnectGame_OnDisconnected(string reason)
    {
        ConnectGame.OnDisconnected -= ConnectGame_OnDisconnected;
        ConnectGame = null;
        LogError($"Disconnected by server: {reason}");
        _ = DisconnectServer();
    }

    #endregion

    #region Player

    private void CreateMainPlayer(IClientPlayer player)
    {
        var character = Instantiate(PlayerPrefab, player.Properties.Position.ToVector(), player.Properties.Rotation.ToQuaternion());
        character.AddComponent<PlayerBehaviour>().Player = player;
        character.name = $"MainPlayer: {player.PlayerId}";
        ConnectCameraToPlayer(character);
        LogInformation("Created Main player");
    }

    private void CreateOtherPlayer(IClientPlayer player)
    {
        var character = Instantiate(PlayerPrefab, player.Properties.Position.ToVector(), player.Properties.Rotation.ToQuaternion());
        character.AddComponent<PlayerBehaviour3rd>().Player = player;
        character.name = $"OtherPlayer: {player.PlayerId}";
        LogInformation($"Created other player {player.PlayerId}");
    }

    private void ConnectCameraToPlayer(GameObject character)
    {
        var followCamera = GameObject.Find("PlayerFollowCamera").GetComponent<CinemachineVirtualCamera>();
        followCamera.Follow = character.FindChildWithTag("CinemachineTarget").transform;
    }

    public string HostId => CurrentRoom.Players.Where(p => p.Properties.IsHost).Select(p => p.PlayerId).FirstOrDefault();

    #endregion

    #region RoomObjects

    public readonly HashSet<RoomObjectBehaviour> SceneRoomObjects = new HashSet<RoomObjectBehaviour>();

    public RoomObjectBehaviour FindRoomObjectComponent(string objectId) =>
        SceneRoomObjects.Where(o => o.RoomObject != null).FirstOrDefault(o => o.RoomObject.ObjectId == objectId);

    public async Task<IClientRoomObject> DetectRoomObject(RoomObjectBehaviour component)
    {
        IClientRoomObject obj = null;

        if (IsInitializingRoom)
        {
            obj = CurrentRoom.Objects.FirstOrDefault(o => o.Tag == component.gameObject.tag && o.Properties.Name == component.gameObject.name);
            if (obj == null && IsJoiningRoom)
                return null;    // non existing objects will be removed from scene on Initialization phase
        }

        if (obj == null)
        {
            //if (IsInitializingRoom && component.gameObject.name != "Box_100x100x100_Prefab (12)")
            //    return null;

            string objectId = Guid.NewGuid().ToString();

            if (!IsInitializingRoom)
                component.gameObject.name = objectId;

            var properties = new RoomObjectProperties();
            component.GetProperties(properties);

            properties.CreatorId = CurrentPlayer.PlayerId;

            switch (component.Mode)
            {
                case RoomObjectMode.ActiveOnHost:
                    if (CurrentPlayer.Properties.IsHost)
                        properties.HostId = CurrentPlayer.PlayerId;
                    break;
                case RoomObjectMode.ActiveOnOwner:
                    if (!IsInitializingRoom)
                        properties.OwnerId = CurrentPlayer.PlayerId;
                    break;
                case RoomObjectMode.DynamicHost:
                    properties.HostId = CurrentPlayer.PlayerId;
                    break;
            }

            var result = await ConnectGame.Service.SpawnObject(new SpawnObjectParameters
            {
                ObjectId = objectId,
                Tag = component.gameObject.tag,
                ObjectProperties = properties,
            });

            if (!result.IsOk)
            {
                LogError(result);
                return null;
            }

            return CurrentRoom.Objects[objectId];
        }
        else
        {
            component.SetProperties(obj, immediate: true);
            return obj;
        }
    }

    public GameObject GetRoomObjectPrefab(string tag) => 
        ObjectPrefabs.Where(p => p.tag == tag).Select(p => p.prefab).FirstOrDefault();

    private void SpawnObject(IClientRoomObject obj)
    {
        if (obj.Properties.CreatorId == CurrentPlayer.PlayerId) return;

        var prefab = GetRoomObjectPrefab(obj.Tag);
        if (prefab == null)
        {
            LogError($"No prefab defined for tag {obj.Tag}");
            return;
        }

        var instance = Instantiate(prefab, obj.Properties.Position.ToVector(), obj.Properties.Rotation.ToQuaternion());
        var component = instance.GetComponent<RoomObjectBehaviour>();
        component.RoomObject = obj;
        component.SetProperties(obj, immediate: true);
    }

    private void ReconfigureRoomObjectsHostChanged()
    {
        foreach (var component in SceneRoomObjects)
        {
            if (component.Mode == RoomObjectMode.ActiveOnHost)
                component.ConfigureMode();
        }
    }

    #endregion

    #region UI

    public string ProgressText { get; set; }
    public string StatusText { get; set; }
    public Color StatusColor { get; set; } = Color.green;

    public void LogError(Result result) => LogError($"{result.StatusName} '{result.Message}'");

    public void LogError(string str)
    {
        StatusColor = Color.red;
        StatusText = str;
        Debug.LogError(str);
    }

    public void LogException(Exception ex)
    {
        StatusColor = Color.red;
        StatusText = ex.Message;
        Debug.LogException(ex);
    }

    public void LogInformation(string str)
    {
        StatusColor = Color.green;
        StatusText = str;
        Debug.Log(str);
    }

    public void ClearProgress() => ProgressText = string.Empty;

    public void LogProgress(string str)
    {
        ProgressText += $"{str}... ";
        Debug.Log(str);
    }

    public void LogProgress(string str, int progress)
    {
        // ProgressText += $"{str} {progress}%\n";
        ProgressText += $"{str}\n";
        Debug.Log(str);
    }

    #endregion
}