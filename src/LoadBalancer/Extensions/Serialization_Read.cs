using LoadBalancer.Common;
using System;
using System.IO;
using System.Text;

namespace LoadBalancer.Extensions
{
    public static partial class Serialization
    {
        public static OperationType ReadOperationType(this BinaryReader reader)
        {
            return (OperationType)reader.ReadByte();
        }

        public static DataType ReadDataType(this BinaryReader reader)
        {
            return reader.ReadByte();
        }

        public static KeyType ReadKeyType(this BinaryReader reader)
        {
#if USE_BYTE_KEYS
            return reader.ReadByte();
#else
            return reader.ReadStringASCII();
#endif
        }

        public static Guid ReadGuid(this BinaryReader reader)
        {
            // how to interpret byte[16] to GUID in C++
            // https://stackoverflow.com/a/68919259
            byte[] bytes = reader.ReadBytes(16);
            return new Guid(bytes);
        }

        public static TimeSpan ReadTimeSpan(this BinaryReader reader)
        {
            long ticks = reader.ReadInt64();
#if USE_UNIX_TIME
            return new TimeSpan(ticks * TimeSpan.TicksPerSecond);
#else
            return new TimeSpan(ticks);
#endif
        }

        public static DateTime ReadDateTime(this BinaryReader reader)
        {
            long ticks = reader.ReadInt64();
#if USE_UNIX_TIME
            return DateTimeOffset.FromUnixTimeSeconds(ticks).UtcDateTime;
#else
            return new DateTime(ticks);
#endif
        }

        public static string ReadStringASCII(this BinaryReader reader)
        {
            byte length = reader.ReadByte();
            var arr = reader.ReadBytes(length);
            return Encoding.ASCII.GetString(arr);
        }

        public static string ReadStringUtf8(this BinaryReader reader)
        {
            var arr = ReadByteArray(reader);
            return Encoding.UTF8.GetString(arr);
        }

        public static Compressed<string> ReadCompressedString(this BinaryReader reader)
        {
            var arr = ReadByteArray(reader);
            return new Compressed<string>(arr.DecompressString());
        }

        public static Compressed<byte[]> ReadCompressedByteArray(this BinaryReader reader)
        {
            var arr = ReadByteArray(reader);
            return new Compressed<byte[]>(arr.DecompressArray());
        }

        public static byte[] ReadByteArray(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            return reader.ReadBytes(length);
        }

        public static Array ReadArray(this BinaryReader reader)
        {
            var type = reader.ReadDataType();
            var serializer = FindSerializer(type);
            int length = reader.ReadInt32();
            var array = Array.CreateInstance(serializer.InstanceType, length);

            for (int i = 0; i < length; i++)
            {
                var value = serializer.ReadValue(reader);
                array.SetValue(value, i);
            }

            return array;
        }

        public static KeyValueCollection ReadDictionary(this BinaryReader reader)
        {
            var collection = new KeyValueCollection();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var name = reader.ReadKeyType();
                object value = reader.ReadObject();

                collection.SetValue(name, value);
            }

            return collection;
        }

        public static object ReadObject(this BinaryReader reader)
        {
            var type = reader.ReadDataType();
            var serializer = FindSerializer(type);
            return serializer.ReadValue(reader);
        }

        public static T BinaryDeserialize<T>(byte[] data, Func<BinaryReader, T> action)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
                return action(reader);
        }

        public static void BinaryDeserialize(byte[] data, Action<BinaryReader> action)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
                action(reader);
        }
    }
}
