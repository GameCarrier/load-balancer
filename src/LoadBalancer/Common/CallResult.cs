namespace LoadBalancer
{
    public enum CallResult : byte
    {
        NotHandled = 0,
        Completed = 1,
        Failed = 2,
        Reenqueued = 3,
    }
}
