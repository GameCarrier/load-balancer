namespace LoadBalancer.Common
{
    public static class SharedSettings
    {
        public static bool RaiseLocalEvents = true;
    }

    public enum CommonErrors : byte
    {
        Ok,
        Error_ConnectException = 255,
        Error_ServerException = 254,
        Error_SerializationException = 253,
        Error_MaterializationException = 252,
    }

    public enum CommonMethods : byte
    {
        Disconnect = 255,
        Echo = 254,
    }

    public enum CommonParameters : byte
    {
        Status = 255,
        Message = 254,
        StatusName = 253,
        Reason = 252,
    }

    public static class CommonMessages
    {
        public const string Message_NotCompleted = "not completed";
        public const string Message_NotEnqueued = "not enqueued";
        public const string Message_AwaitingDisconnect = "awaiting disconnect";
        public const string Message_AwaitingConnect = "awaiting connect";
        public const string Message_Disconnected = "disconnected";
        public const string Message_CantConnect = "can't connect";
        public const string Message_TimedOut = "timed out";
    }
}
