namespace LoadBalancer.Common
{
    using System;
#if USE_BYTE_KEYS
    using System.Reflection;

    public struct KeyType
    {
        public static readonly KeyType Empty = new KeyType { Value = 0 };
        public byte Value { get; private set; }
        public string ToDisplayName(Type type) => 
            type != null ? Enum.GetName(type, Value) : ToString();
        public T As<T>() where T : struct => (T)Enum.ToObject(typeof(T), Value);

        public override string ToString() => Value.ToString();
        public override int GetHashCode() => Value;
        public override bool Equals(object obj) => obj is KeyType n && Value == n.Value;

        public static implicit operator KeyType(byte value) => new KeyType { Value = value };
        public static implicit operator KeyType(Enum value) => new KeyType { Value = Convert.ToByte(value) };
        public static bool operator ==(KeyType n1, KeyType n2) => n1.Value == n2.Value;
        public static bool operator !=(KeyType n1, KeyType n2) => n1.Value != n2.Value;

        public static KeyType GetMemberKey(MemberInfo member)
        {
            var attribute = member.GetCustomAttribute<KeyTypeAttribute>();
            return attribute != null ? (KeyType)attribute.Code : Empty;
        }
    }
#else
    public struct KeyType
    {
        public static readonly KeyType Empty = new KeyType { Value = string.Empty };
        public string Value { get; private set; }
        public string ToDisplayName(Type type) =>
            type != null && string.IsNullOrEmpty(Value) ? Enum.GetName(type, 0) : ToString();
        public T As<T>() where T : struct => string.IsNullOrEmpty(Value)
            ? (T)Enum.ToObject(typeof(T), 0) : (T)Enum.Parse(typeof(T), Value);

        public override string ToString() => Value;
        public override int GetHashCode() => Value.GetHashCode();
        public override bool Equals(object obj) => obj is KeyType n && Value == n.Value;

        public static implicit operator KeyType(string value) => new KeyType { Value = value };
        public static implicit operator KeyType(Enum value) => new KeyType { Value = value.ToString() };
        public static bool operator ==(KeyType n1, KeyType n2) => n1.Value == n2.Value;
        public static bool operator !=(KeyType n1, KeyType n2) => n1.Value != n2.Value;
    }
#endif
}
