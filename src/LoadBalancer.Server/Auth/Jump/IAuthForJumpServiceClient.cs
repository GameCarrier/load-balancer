namespace LoadBalancer.Server.Auth.Jump
{
    public interface IAuthForJumpServiceClient
    {
        void OnJumpServiceAdded(AddJumpServiceParameters parameters);
    }
}
