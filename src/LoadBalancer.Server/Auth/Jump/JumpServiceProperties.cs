using LoadBalancer.Common;

namespace LoadBalancer.Server.Auth.Jump
{
    public class JumpServiceProperties : KeyValueCollection
    {
        enum JumpServiceKeys : byte
        {
            Region,
            TitleId,
            Version,
        }

        public string Region
        {
            get => GetValue<string>(JumpServiceKeys.Region);
            set => SetValue(JumpServiceKeys.Region, value);
        }

        public string TitleId
        {
            get => GetValue<string>(JumpServiceKeys.TitleId);
            set => SetValue(JumpServiceKeys.TitleId, value);
        }

        public string Version
        {
            get => GetValue<string>(JumpServiceKeys.Version);
            set => SetValue(JumpServiceKeys.Version, value);
        }
    }
}
