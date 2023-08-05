namespace LoadBalancer.Game
{
    public enum GameErrors : byte
    {
        Ok,
        Error_ProviderNotSupported,
        Error_ParameterMissed,
        Error_TokenInvalid,
        Error_NotAuthenticated,
        Error_RoomNotFound,
        Error_RoomAlreadyCreated,
        Error_PlayerNotFound,
        Error_PlayerAlreadyJoined,
        Error_ObjectAlreadySpawned,
        Error_ObjectNotFound,
        Error_RoomFull,
    }

    public enum GameMethods : byte
    {
        Authenticate,
        CreateRoom,
        JoinRoom,
        UpdateRoom,
        UpdatePlayer,
        RaiseRoomEvent,
        SpawnObject,
        UpdateObject,
        DestroyObject,

        OnRoomUpdated,
        OnPlayerUpdated,
        OnRoomJoined,
        OnRoomLeaved,
        OnRoomEventRaised,
        OnObjectSpawned,
        OnObjectUpdated,
        OnObjectDestroyed,

        OnJumpServiceConnectEnabled,
        OnJumpServiceConnectDisabled,

        // RoomEvents
        ApplyForce,
    }

    public enum PlayerKeys : byte
    {
        UserId,
        Nickname,

        IsHost,
        Level,
        Position,
        Rotation,
        MoveDirection,
        IsSprint,
        IsJump,
    }

    public enum RoomKeys : byte
    {
        MaxPlayers,
        IsPrivate,
        SceneName,
    }

    public enum RoomObjectKeys : byte
    {
        CreatorId,
        OwnerId,
        HostId,
        Name,
        Position,
        Rotation,
        Velocity,
        AngularVelocity,
    }

    public enum RoomEventKeys : byte
    {
        ObjectId,
        Force,
        Times,
        Interval,
    }
}
