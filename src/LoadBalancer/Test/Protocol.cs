namespace LoadBalancer.Test
{
    public enum TestMethods
    {
        Incompleted,
        NestedIncompleted,
        Exception,
        ResultException,
        NestedException,
        NestedResultException,
        MarkSendFromDisconnect,
        CallFromDisconnect,
    }

    public enum TestErrors
    {
        Error_Test,
    }

    public enum TestKeys : byte
    {
        Count,
    }
}
