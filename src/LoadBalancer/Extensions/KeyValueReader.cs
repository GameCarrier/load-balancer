using LoadBalancer.Common;
using System;
using System.IO;

namespace LoadBalancer.Extensions
{
    public class KeyValueReader : IDisposable
    {
        private readonly BinaryReader reader;
        private readonly long startPosition;
        private int index = 0;
        private int count;

        public bool IsStart => index == 0;

        public bool IsEnd => index > count;

        public KeyValueReader(BinaryReader reader)
        {
            this.reader = reader;
            startPosition = reader.BaseStream.Position;
        }

        public T GetValue<T>(KeyType key)
        {
            try
            {
                T value = default;
                int currentIndex = index;
                do
                {
                    if (IsEnd) { MoveToStart(); continue; }

                    if (IsStart) { ReadLength(); continue; }

                    if (!IsEnd)
                    {
                        var name = reader.ReadKeyType();
                        var type = reader.ReadDataType();
                        var serializer = Serialization.FindSerializer(type);
                        if (name == key)
                        {
                            if (serializer is Serialization.DataSerializer<T> valueSerializer)
                                value = valueSerializer.Read(reader);
                            else
                            {
                                var baseValue = serializer.ReadValue(reader);
                                value = (T)Conversion.ConvertSmart(baseValue, typeof(T));
                            }
                            index++;
                            break;
                        }
                        else
                        {
                            serializer.SkipValue(reader);
                            index++;
                            continue;
                        }
                    }
                }
                while (currentIndex != index);

                return value;
            }
            catch
            {
                MoveToStart();
                throw;
            }
        }

        private void MoveToStart()
        {
            reader.BaseStream.Position = startPosition;
            index = 0;
        }

        private void ReadLength()
        {
            count = reader.ReadInt32();
            index = 1;
        }

        private void MoveToEnd()
        {
            try
            {
                while (!IsEnd)
                {
                    if (IsStart)
                        ReadLength();
                    else
                    {
                        reader.SkipKeyType();
                        reader.SkipObject();
                        index++;
                    }
                }
            }
            catch
            {
                MoveToStart();
                throw;
            }
        }

        public void Dispose()
        {
            MoveToEnd();
        }
    }
}
