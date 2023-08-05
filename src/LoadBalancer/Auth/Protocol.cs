namespace LoadBalancer.Auth
{
    public enum AuthErrors : byte
    {
        Ok,
        Error_ProviderNotSupported,
        Error_ParameterMissed,
        Error_NotAuthenticated,
        Error_JumpServerNotFound,
    }

    public enum AuthMethods : byte
    {
        Authenticate,
        ListJumpServices,

        // S2S for Jump
        OnJumpServiceAdded,
    }

    public enum AuthParameters : byte
    {
        UserName,
        Password,

        UserId,
        LanguageId,
    }
}
