using LoadBalancer.Common;

namespace LoadBalancer.Tests
{
    public class TestKeyValueMap : KeyValueCollection
    {
        enum TestKeys
        {
            Name = 1,
            Age = 2,
        }

        public string Name
        {
            get => GetValue<string>(TestKeys.Name);
            set => SetValue(TestKeys.Name, value);
        }

        public int Age
        {
            get => GetValue<int>(TestKeys.Age);
            set => SetValue(TestKeys.Age, value);
        }
    }
}
