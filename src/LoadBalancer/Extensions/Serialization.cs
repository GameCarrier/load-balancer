namespace LoadBalancer.Extensions
{
    public static partial class Serialization
    {
        public static class DataTypes
        {
            public static readonly DataType Null = 0;

            // 1 byte
            public static readonly DataType Boolean = 1;
            public static readonly DataType Char = 2;
            public static readonly DataType Byte = 3;
            public static readonly DataType SByte = 4;

            // 2 bytes
            public static readonly DataType Int16 = 5;
            public static readonly DataType UInt16 = 6;

            // 4 bytes
            public static readonly DataType Float = 7;
            public static readonly DataType Int32 = 8;
            public static readonly DataType UInt32 = 9;

            // 8 bytes
            public static readonly DataType Double = 10;
            public static readonly DataType Int64 = 11;
            public static readonly DataType UInt64 = 12;

            // 16 bytes
            public static readonly DataType Decimal = 13;

            // Structures
            public static readonly DataType KeyType = 21;
            public static readonly DataType Guid = 22;
            public static readonly DataType TimeSpan = 23;
            public static readonly DataType DateTime = 24;

            // Strings & Byte Arrays
            public static readonly DataType String = 31;
            public static readonly DataType CompressedString = 32;
            public static readonly DataType ByteArray = 33;
            public static readonly DataType CompressedByteArray = 34;

            // Dynamic Types
            public static readonly DataType Object = 41;
            public static readonly DataType Array = 42;
            public static readonly DataType Dictionary = 43;
        }

        static Serialization()
        {
            CreateSerializer(DataTypes.Null, r => (object)null, (w, o) => { })
                .InitSkip(0).RegisterSerializer();

            CreateSerializer(DataTypes.Boolean, r => r.ReadBoolean(), (w, o) => w.Write(o))
                .InitSkip(sizeof(bool)).RegisterSerializer();

            CreateSerializer(DataTypes.Char, r => (char)r.ReadByte(), (w, o) => w.Write((byte)o))
                .InitSkip(1 /* sizeof(char) returns 2 */).RegisterSerializer();

            CreateSerializer(DataTypes.Byte, r => r.ReadByte(), (w, o) => w.Write(o))
                .InitSkip(sizeof(byte)).RegisterSerializer();

            CreateSerializer(DataTypes.SByte, r => r.ReadSByte(), (w, o) => w.Write(o))
                .InitSkip(sizeof(sbyte)).RegisterSerializer();

            CreateSerializer(DataTypes.Int16, r => r.ReadInt16(), (w, o) => w.Write(o))
                .InitSkip(sizeof(short)).RegisterSerializer();

            CreateSerializer(DataTypes.UInt16, r => r.ReadUInt16(), (w, o) => w.Write(o))
                .InitSkip(sizeof(ushort)).RegisterSerializer();

            CreateSerializer(DataTypes.Float, r => r.ReadSingle(), (w, o) => w.Write(o))
                .InitSkip(sizeof(float)).RegisterSerializer();

            CreateSerializer(DataTypes.Int32, r => r.ReadInt32(), (w, o) => w.Write(o))
                .InitSkip(sizeof(int)).RegisterSerializer();

            CreateSerializer(DataTypes.UInt32, r => r.ReadUInt32(), (w, o) => w.Write(o))
                .InitSkip(sizeof(uint)).RegisterSerializer();

            CreateSerializer(DataTypes.Double, r => r.ReadDouble(), (w, o) => w.Write(o))
                .InitSkip(sizeof(double)).RegisterSerializer();

            CreateSerializer(DataTypes.Int64, r => r.ReadInt64(), (w, o) => w.Write(o))
                .InitSkip(sizeof(long)).RegisterSerializer();

            CreateSerializer(DataTypes.UInt64, r => r.ReadUInt64(), (w, o) => w.Write(o))
                .InitSkip(sizeof(ulong)).RegisterSerializer();

            CreateSerializer(DataTypes.Decimal, r => r.ReadDecimal(), (w, o) => w.Write(o))
                .InitSkip(sizeof(decimal)).RegisterSerializer();

            CreateSerializer(DataTypes.KeyType, r => r.ReadKeyType(),
                (w, o) => w.WriteKeyType(o))
                .InitSkip(SkipKeyType).RegisterSerializer();

            CreateSerializer(DataTypes.Guid, r => r.ReadGuid(), (w, o) => w.WriteGuid(o))
                .InitSkip(16).RegisterSerializer();

            CreateSerializer(DataTypes.TimeSpan, r => r.ReadTimeSpan(), (w, o) => w.WriteTimeSpan(o))
                .InitSkip(sizeof(long)).RegisterSerializer();

            CreateSerializer(DataTypes.DateTime, r => r.ReadDateTime(), (w, o) => w.WriteDateTime(o))
                .InitSkip(sizeof(long)).RegisterSerializer();

            CreateSerializer(DataTypes.String, r => r.ReadStringUtf8(), (w, o) => w.WriteStringUtf8(o))
                .InitSkip(SkipLength).RegisterSerializer();

            CreateSerializer(DataTypes.CompressedString, r => r.ReadCompressedString(),
                (w, o) => w.WriteCompressedString(o))
                .InitSkip(SkipLength).RegisterSerializer();

            CreateSerializer(DataTypes.ByteArray, r => r.ReadByteArray(), (w, o) => w.WriteByteArray(o))
                .InitSkip(SkipLength).RegisterSerializer();

            CreateSerializer(DataTypes.CompressedByteArray, r => r.ReadCompressedByteArray(),
                (w, o) => w.WriteCompressedByteArray(o))
                .InitSkip(SkipLength).RegisterSerializer();

            CreateSerializer(DataTypes.Object, r => r.ReadObject(), (w, o) => w.WriteObject(o))
                .InitSkip(SkipObject).RegisterSerializer();

            CreateSerializer(DataTypes.Array, r => r.ReadArray(), (w, o) => w.WriteArray(o))
                .InitSkip(SkipArray).RegisterSerializer();

            CreateSerializer(DataTypes.Dictionary, r => r.ReadDictionary(), (w, o) => w.WriteDictionary(o))
                .InitSkip(SkipDictionary).RegisterSerializer();
        }
    }
}
