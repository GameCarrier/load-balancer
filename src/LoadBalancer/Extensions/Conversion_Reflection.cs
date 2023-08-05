using LoadBalancer.Common;
using System;
using System.Threading.Tasks;

namespace LoadBalancer.Extensions
{

    public static partial class Conversion
    {
        public static async Task<KeyValueCollection> SerializeAsync(this object obj)
        {
            if (obj == null) return null;
            return await Task.Run(() => Serialize(obj));
        }

        public static KeyValueCollection Serialize(this object obj)
        {
            if (obj == null) return null;

            if (obj is KeyValueCollection kv)
                return kv;

            if (MetadataTable.IsReflectionSerializationUsedForType(obj.GetType()))
                return SerializeReflectionMap(obj);

            if (obj is IKeyValueMap keyValueMap)
                return SerializeKeyValueMap(keyValueMap);

            throw new ArgumentException($"Can't serialize {obj} to KeyValueCollection");
        }

        private static KeyValueCollection SerializeReflectionMap(object obj)
        {
            var snapshot = new KeyValueCollection();

            foreach (var entry in MetadataTable.GetSerializationProperties(obj.GetType()))
            {
                var property = entry.Property;
                var key = entry.Key;

                var value = property.GetValue(obj);
                try
                {
                    value = SerializeProperty(value);
                    if (value != null)
                        snapshot.SetValue(key, value);
                }
                catch (ResultException ex)
                {
                    ex.AddCallContext($" for {obj.GetType().Name}.{property.Name}");
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ResultException(CommonErrors.Error_SerializationException, ex.Message)
                        .AddCallContext($" for {obj.GetType().Name}.{property.Name}");
                }
            }
            return snapshot;
        }

        private static object SerializeProperty(object value)
        {
            if (value == null)
                return null;

            if (value.GetType() == typeof(KeyValueCollection))
                return value;

            if (value.GetType().IsArray)
            {
                var arr = (Array)value;
                var elementType = value.GetType().GetElementType();

                if (typeof(KeyValueCollection).IsAssignableFrom(elementType))
                {
                    var result = new KeyValueCollection[arr.Length];
                    for (int i = 0; i < arr.Length; i++)
                        result[i] = (KeyValueCollection)arr.GetValue(i);
                    return result;
                }

                if (Serialization.HasSerializerForType(elementType))
                    return value;

                if (MetadataTable.IsReflectionSerializationUsedForType(elementType))
                {
                    var result = new KeyValueCollection[arr.Length];
                    for (int i = 0; i < arr.Length; i++)
                        result[i] = SerializeReflectionMap(arr.GetValue(i));
                    return result;
                }

                if (typeof(IKeyValueMap).IsAssignableFrom(elementType))
                {
                    var result = new KeyValueCollection[arr.Length];
                    for (int i = 0; i < arr.Length; i++)
                        result[i] = SerializeKeyValueMap((IKeyValueMap)arr.GetValue(i));
                    return result;
                }
            }
            else
            {
                if (value is KeyValueCollection kv)
                    return kv;

                if (MetadataTable.IsReflectionSerializationUsedForType(value.GetType()))
                    return SerializeReflectionMap(value);

                if (value is IKeyValueMap keyValueMap)
                    return SerializeKeyValueMap(keyValueMap);
            }

            if (value is ICompressed compressed)
            {
                var innerValue = SerializeProperty(compressed.Value);
                byte[] data = Serialization.BinarySerialize(writer => writer.WriteObject(innerValue));
                return Compression.CompressArray(data);
            }

            if (Serialization.HasSerializerForType(value.GetType()))
                return value;

            throw new ResultException(CommonErrors.Error_SerializationException, $"Serialization of type {value.GetType()} not supported");
        }

        public static async Task<T> MaterializeAsync<T>(this KeyValueCollection snapshot) where T : new()
        {
            if (snapshot == null) return default;
            return await Task.Run(() => Materialize<T>(snapshot));
        }

        public static async Task<object> MaterializeAsync(this KeyValueCollection snapshot, Type type)
        {
            if (snapshot == null) return default;
            return await Task.Run(() => Materialize(snapshot, type));
        }

        public static T Materialize<T>(this KeyValueCollection snapshot) where T : new()
            => (T)Materialize(snapshot, typeof(T));

        public static object Materialize(this KeyValueCollection snapshot, Type type)
        {
            if (snapshot == null) return default;

            if (MetadataTable.IsReflectionSerializationUsedForType(type))
                return MaterializeReflectionMap(snapshot, Activator.CreateInstance(type));

            if (typeof(IKeyValueMap).IsAssignableFrom(type))
                return MaterializeKeyValueMap(snapshot, (IKeyValueMap)Activator.CreateInstance(type));

            throw new ArgumentException($"Can't materialize KeyValueCollection to {type}");
        }

        private static object MaterializeReflectionMap(KeyValueCollection snapshot, object obj)
        {
            foreach (var entry in MetadataTable.GetSerializationProperties(obj.GetType()))
            {
                var property = entry.Property;
                var key = entry.Key;
                if (!snapshot.TryGetValue(key, out var value)) continue;

                try
                {
                    value = MaterializeProperty(value, property.PropertyType);
                    property.SetValue(obj, value);
                }
                catch (ResultException ex)
                {
                    ex.AddCallContext($" for {obj.GetType().Name}.{property.Name}");
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ResultException(CommonErrors.Error_MaterializationException, ex.Message)
                        .AddCallContext($" for {obj.GetType().Name}.{property.Name}");
                }
            }
            return obj;
        }

        private static object MaterializeProperty(object value, Type type)
        {
            if (value == null)
                return null;

            if (value is KeyValueCollection kv)
            {
                if (MetadataTable.IsReflectionSerializationUsedForType(type))
                    return MaterializeReflectionMap(kv, Activator.CreateInstance(type));

                if (typeof(IKeyValueMap).IsAssignableFrom(type))
                    return MaterializeKeyValueMap(kv, (IKeyValueMap)Activator.CreateInstance(type));
            }

            if (value is KeyValueCollection[] arr && type.IsArray)
            {
                var elementType = type.GetElementType();

                if (MetadataTable.IsReflectionSerializationUsedForType(elementType))
                {
                    var result = Array.CreateInstance(elementType, arr.Length);
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var element = MaterializeReflectionMap(arr[i], Activator.CreateInstance(elementType));
                        result.SetValue(element, i);
                    }
                    return result;
                }

                if (typeof(IKeyValueMap).IsAssignableFrom(elementType))
                {
                    var result = Array.CreateInstance(elementType, arr.Length);
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var element = MaterializeKeyValueMap(arr[i], (IKeyValueMap)Activator.CreateInstance(elementType));
                        result.SetValue(element, i);
                    }
                    return result;
                }
            }

            if (typeof(ICompressed).IsAssignableFrom(type))
            {
                var elementType = type.GetGenericArguments()[0];
                byte[] data = Compression.DecompressArray((byte[])value);
                object innerValue = Serialization.BinaryDeserialize(data, reader => reader.ReadObject());
                innerValue = MaterializeProperty(innerValue, elementType);
                return Activator.CreateInstance(type, innerValue);
            }

            try
            {
                return ConvertSmart(value, type);
            }
            catch (Exception ex)
            {
                throw new ResultException(CommonErrors.Error_MaterializationException, $"Failed to convert value '{value}' to {type} - {ex.Message}");
            }
        }
    }
}
