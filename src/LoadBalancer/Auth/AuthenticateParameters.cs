using LoadBalancer.Common;

namespace LoadBalancer.Auth
{
    [ReflectionSerialization]
    public class AuthenticateParameters
    {
        public string Provider { get; set; }
        public string Token { get; set; }
        public KeyValueCollection Params { get; set; } = new KeyValueCollection();

        [AvoidSerialization]
        public string UserName
        {
            get => Params.GetValue<string>(AuthParameters.UserName);
            set => Params.SetValue(AuthParameters.UserName, value);
        }
        [AvoidSerialization]
        public string Password
        {
            get => Params.GetValue<string>(AuthParameters.Password);
            set => Params.SetValue(AuthParameters.Password, value);
        }
    }
}
