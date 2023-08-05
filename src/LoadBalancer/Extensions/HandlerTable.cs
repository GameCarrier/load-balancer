using LoadBalancer.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace LoadBalancer.Extensions
{
    public interface ICallContext
    {
        KeyType Name { get; }
        KeyValueCollection Parameters { get; }
        void Accept();

        HandlerTable Metadata { get; set; }
    }

    public class HandlerTable
    {
        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<HandlerTable>();
        private static ConcurrentDictionary<Type, HandlerTable> metadata = new ConcurrentDictionary<Type, HandlerTable>();

        class Entry
        {
            public MethodInfo Method;
            public Type ParameterType;

            public override string ToString() => $"{Method.Name} ({ParameterType})";
        }

        private Type MethodType;
        private Type ErrorType;
        private Dictionary<KeyType, Entry> table = new Dictionary<KeyType, Entry>();

        private HandlerTable Fill(Type type)
        {
            Logger.LogDebug($"FillHandlerTable for {type}");

            MethodType = type.GetCustomAttribute<MethodsEnumAttribute>()?.Type;
            ErrorType = type.GetCustomAttribute<ErrorsEnumAttribute>()?.Type;

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                var parameters = method.GetParameters();
                if (typeof(CallResult).Equals(method.ReturnType)
                    && parameters.Length >= 1 && parameters.Length <= 2
                    && typeof(ICallContext).IsAssignableFrom(parameters[0].ParameterType))
                {
#if USE_BYTE_KEYS
                    KeyType key = KeyType.GetMemberKey(method);
                    if (key == KeyType.Empty)
                        key = GetMethodCode(method.Name);
#else
                    KeyType key = method.Name;
#endif
                    table.Add(key, new Entry
                    {
                        Method = method,
                        ParameterType = parameters.Length > 1 ? parameters[1].ParameterType : null
                    });
                }
            }

            return this;
        }

        public static void ParseMetadata(object handler, ICallContext call)
        {
            if (call.Metadata == null)
                call.Metadata = metadata.GetOrAdd(handler.GetType(), t => new HandlerTable().Fill(t));
        }

        public static CallResult ExecuteHandler(object handler, ICallContext call)
        {
            ParseMetadata(handler, call);
            var table = call.Metadata;

            if (table.table.TryGetValue(call.Name, out var entry))
            {
                call.Accept();
                try
                {
                    if (entry.ParameterType != null)
                        return (CallResult)entry.Method.Invoke(handler,
                            new object[] { call, call.Parameters.Materialize(entry.ParameterType) });

                    return (CallResult)entry.Method.Invoke(handler, new object[] { call });
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }
            return CallResult.NotHandled;
        }

        private byte GetMethodCode(string name) =>
            MethodType != null && Enum.IsDefined(MethodType, name)
                ? Convert.ToByte(Enum.Parse(MethodType, name))
                : (byte)0;

        public static Type GetMethodType(ICallContext call) =>
            call.Metadata?.MethodType ?? typeof(CommonMethods);

        public static Type GetErrorType(ICallContext call) =>
            call.Metadata?.ErrorType ?? typeof(CommonErrors);
    }
}
