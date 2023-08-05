using LoadBalancer.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace LoadBalancer.Extensions
{
    public static partial class Serialization
    {
        private static readonly IDataSerializer[] serializers = new IDataSerializer[255];
        private static readonly Dictionary<Type, IDataSerializer> serializersReverse = new Dictionary<Type, IDataSerializer>();

        public static DataSerializer<T> CreateSerializer<T>(DataType type,
            Func<BinaryReader, T> read, Action<BinaryWriter, T> write)
        {
            return new DataSerializer<T>(type, read, write);
        }

        public static void RegisterSerializer(this IDataSerializer serializer)
        {
            //if (serializers[serializer.DataType.Code] != null)
            //    throw new ArgumentException($"Serializer with code {serializer.DataType.Code} already registered");
            serializers[serializer.DataType.Code] = serializer;
            serializersReverse[serializer.InstanceType] = serializer;
        }

        public static IDataSerializer FindSerializer(DataType type)
        {
            var propertySerializer = serializers[type.Code];
            if (propertySerializer != null)
                return propertySerializer;

            throw new NotImplementedException($"{type}: serializer not supported");
        }

        private static IDataSerializer FindSerializer(object obj)
        {
            if (obj == null)
                return FindSerializer(DataTypes.Null);

            if (obj is KeyValueCollection)
                return FindSerializer(DataTypes.Dictionary);

            if (obj is string)
                return FindSerializer(DataTypes.String);

            if (obj is byte[])
                return FindSerializer(DataTypes.ByteArray);

            if (obj is Array)
                return FindSerializer(DataTypes.Array);

            var type = obj.GetType();
            if (type == typeof(object))
                return FindSerializer(DataTypes.Object);

            if (serializersReverse.TryGetValue(type, out var propertySerializer))
                return propertySerializer;

            throw new NotImplementedException($"{type}: serializer can't be determined");
        }

        private static IDataSerializer FindSerializer(Type type)
        {
            if (type == typeof(KeyValueCollection))
                return FindSerializer(DataTypes.Dictionary);

            if (type == typeof(string))
                return FindSerializer(DataTypes.String);

            if (type == typeof(byte[]))
                return FindSerializer(DataTypes.ByteArray);

            if (typeof(Array).IsAssignableFrom(type))
                return FindSerializer(DataTypes.Array);

            if (type == typeof(object))
                return FindSerializer(DataTypes.Object);

            if (serializersReverse.TryGetValue(type, out var propertySerializer))
                return propertySerializer;

            throw new NotImplementedException($"{type}: serializer can't be determined");
        }

        public static bool HasSerializerForType(Type type)
        {
            if (type == typeof(object))
                throw new ArgumentException("type should be specified");

            return serializersReverse.ContainsKey(type);
        }
    }
}
