using LoadBalancer.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LoadBalancer.Extensions
{
    public class MetadataTable
    {
        private static readonly ILogger Logger = ServiceFactory.Instance.GetLogger<MetadataTable>();
        private static ConcurrentDictionary<Type, MetadataTable> metadata = new ConcurrentDictionary<Type, MetadataTable>();
        private static ConcurrentDictionary<Type, bool> reflectionEnabled = new ConcurrentDictionary<Type, bool>();

        public class Entry
        {
            public PropertyInfo Property;
            public KeyType Key;

            public override string ToString() => $"{Property.Name} ({Key})";
        }

        private List<Entry> table = new List<Entry>();

        private MetadataTable Fill(Type type)
        {
            Logger.LogDebug($"FillSerializationProperties for {type}");
#if USE_BYTE_KEYS
            byte index = 0;
#endif
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => p.GetCustomAttribute<AvoidSerializationAttribute>() == null))
            {
#if USE_BYTE_KEYS
                KeyType key = KeyType.GetMemberKey(property);
                if (key == KeyType.Empty)
                    key = index++;
                else
                    index = (byte)(key.Value + 1);
#else
                KeyType key = property.Name;
#endif
                table.Add(new Entry { Property = property, Key = key });
            }

            return this;
        }

        public static IEnumerable<Entry> GetSerializationProperties(Type type)
        {
            var table = metadata.GetOrAdd(type, t => new MetadataTable().Fill(t));
            return table.table;
        }

        public static bool IsReflectionSerializationUsedForType(Type type) =>
            reflectionEnabled.GetOrAdd(type, t => t.GetCustomAttribute<ReflectionSerializationAttribute>() != null);
    }
}
