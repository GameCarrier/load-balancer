using System.IO;

namespace LoadBalancer.Extensions
{
    public static partial class Serialization
    {
        public static void SkipFixed(this BinaryReader reader, int length)
        {
            reader.BaseStream.Position += length;
        }

        public static void SkipLength(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            reader.BaseStream.Position += length;
        }

        public static void SkipByteLength(this BinaryReader reader)
        {
            byte length = reader.ReadByte();
            reader.BaseStream.Position += length;
        }

        public static void SkipKeyType(this BinaryReader reader)
        {
#if USE_BYTE_KEYS
            SkipFixed(reader, sizeof(byte));
#else
            SkipByteLength(reader);
#endif
        }

        public static void SkipObject(this BinaryReader reader)
        {
            var type = reader.ReadDataType();
            var serializer = FindSerializer(type);
            serializer.SkipValue(reader);
        }

        public static void SkipArray(this BinaryReader reader)
        {
            var type = reader.ReadDataType();
            var serializer = FindSerializer(type);
            int length = reader.ReadInt32();

            for (int i = 0; i < length; i++)
                serializer.SkipValue(reader);
        }

        public static void SkipDictionary(this BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                reader.SkipLength();
                reader.SkipObject();
            }
        }
    }
}
