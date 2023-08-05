namespace LoadBalancer.Jump
{
    public enum JumpErrors : byte
    {
        Ok,
        Error_ProviderNotSupported,
        Error_ParameterMissed,
        Error_TokenInvalid,
        Error_NotAuthenticated,
        Error_WrongTitle,
        Error_WrongVersion,
        Error_RoomNotFound,
        Error_ServerFull,
    }

    public enum JumpMethods : byte
    {
        Authenticate,
        FindRoom,
        FindServer,

        OnGameServiceAdded,
        OnGameServiceUpdated,

        // S2S for Game
        OnRoomPublished,
        OnRoomCreated,
        OnRoomJoined,
        OnRoomLeaved,
        OnRoomUpdated,
        OnPlayerUpdated,
    }
}
