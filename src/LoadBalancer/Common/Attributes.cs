using System;

namespace LoadBalancer.Common
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class AvoidSerializationAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class ReflectionSerializationAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class KeyTypeAttribute : Attribute
    {
        public byte Code { get; private set; }
        public KeyTypeAttribute(byte code) => Code = code;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MethodsEnumAttribute : Attribute
    {
        public Type Type { get; private set; }
        public MethodsEnumAttribute(Type type) => Type = type;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ErrorsEnumAttribute : Attribute
    {
        public Type Type { get; private set; }
        public ErrorsEnumAttribute(Type type) => Type = type;
    }
}
