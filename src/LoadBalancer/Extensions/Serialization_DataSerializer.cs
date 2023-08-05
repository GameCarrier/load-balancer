using System;
using System.IO;

namespace LoadBalancer.Extensions
{
    public static partial class Serialization
    {
        public struct DataType
        {
            public byte Code { get; private set; }

            public static implicit operator DataType(byte code) { return new DataType { Code = code }; }
        }

        public interface IDataSerializer
        {
            DataType DataType { get; }

            Type InstanceType { get; }

            int? Size { get; }

            object ReadValue(BinaryReader reader);

            void WriteValue(BinaryWriter writer, object obj);

            void SkipValue(BinaryReader reader);
        }

        public class DataSerializer<T> : IDataSerializer
        {
            public DataType DataType { get; private set; }

            public Func<BinaryReader, T> Read { get; private set; }

            public Action<BinaryWriter, T> Write { get; private set; }

            public Action<BinaryReader> Skip { get; private set; }

            public int? Size { get; private set; }

            public DataSerializer(DataType dataType, Func<BinaryReader, T> read, Action<BinaryWriter, T> write)
            {
                if (read == null)
                    throw new ArgumentNullException("read");

                if (write == null)
                    throw new ArgumentNullException("write");

                DataType = dataType;
                Read = read;
                Write = write;
                Skip = r => Read(r);
            }

            public DataSerializer<T> InitSkip(Action<BinaryReader> skip)
            {
                if (skip == null)
                    throw new ArgumentNullException("skip");
                Skip = skip;
                return this;
            }

            public DataSerializer<T> InitSkip(int size)
            {
                Size = size;
                Skip = r => r.SkipFixed(size);
                return this;
            }

            Type IDataSerializer.InstanceType => typeof(T);

            object IDataSerializer.ReadValue(BinaryReader reader) => Read(reader);

            void IDataSerializer.WriteValue(BinaryWriter writer, object obj) => Write(writer, (T)obj);

            void IDataSerializer.SkipValue(BinaryReader reader) => Skip(reader);
        }
    }
}
