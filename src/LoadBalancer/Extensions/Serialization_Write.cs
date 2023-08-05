using LoadBalancer.Common;
using System;
using System.IO;
using System.Text;

namespace LoadBalancer.Extensions
{
    public static partial class Serialization
    {
        public static void WriteOperationType(this BinaryWriter writer, OperationType type)
        {
            writer.Write((byte)type);
        }

        public static void WriteDataType(this BinaryWriter writer, DataType type)
        {
            writer.Write(type.Code);
        }

        public static void WriteKeyType(this BinaryWriter writer, KeyType type)
        {
#if USE_BYTE_KEYS
            writer.Write(type.Value);
#else
            writer.WriteStringASCII(type.Value);
#endif
        }

        public static void WriteGuid(this BinaryWriter writer, Guid guid)
        {
            writer.Write(guid.ToByteArray());
        }

        public static void WriteTimeSpan(this BinaryWriter writer, TimeSpan timeSpan)
        {
#if USE_UNIX_TIME
            long ticks = timeSpan.Ticks / TimeSpan.TicksPerSecond;
#else
            long ticks = timeSpan.Ticks;
#endif
            writer.Write(ticks);
        }

        public static void WriteDateTime(this BinaryWriter writer, DateTime dateTime)
        {
#if USE_UNIX_TIME
            long ticks = ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
#else
            long ticks = dateTime.Ticks;
#endif
            writer.Write(ticks);
        }

        public static void WriteStringASCII(this BinaryWriter writer, string str)
        {
            var arr = Encoding.ASCII.GetBytes(str);
            writer.Write((byte)arr.Length);
            writer.Write(arr);
        }

        public static void WriteStringUtf8(this BinaryWriter writer, string str) =>
            WriteByteArray(writer, Encoding.UTF8.GetBytes(str));

        public static void WriteCompressedString(this BinaryWriter writer, Compressed<string> str) =>
            WriteByteArray(writer, str.Value.CompressString());

        public static void WriteCompressedByteArray(this BinaryWriter writer, Compressed<byte[]> arr) =>
            WriteByteArray(writer, arr.Value.CompressArray());

        public static void WriteByteArray(this BinaryWriter writer, byte[] arr)
        {
            writer.Write(arr.Length);
            writer.Write(arr);
        }

        public static void WriteArray(this BinaryWriter writer, Array array)
        {
            var serializer = FindSerializer(array.GetType().GetElementType());
            writer.WriteDataType(serializer.DataType);
            writer.Write(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                var value = array.GetValue(i);
                serializer.WriteValue(writer, value);
            }
        }

        public static void WriteDictionary(this BinaryWriter writer, KeyValueCollection collection)
        {
            writer.Write(collection.Count);
            foreach (var pair in collection)
            {
                writer.WriteKeyType(pair.Key);
                writer.WriteObject(pair.Value);
            }
        }

        public static void WriteObject(this BinaryWriter writer, object obj)
        {
            var serializer = FindSerializer(obj);
            writer.WriteDataType(serializer.DataType);
            serializer.WriteValue(writer, obj);
        }

        public static byte[] BinarySerialize(Action<BinaryWriter> action)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                action(writer);
                return stream.ToArray();
            }
        }
    }
}
