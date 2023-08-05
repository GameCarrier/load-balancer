using System.IO;
using System.IO.Compression;
using System.Text;

namespace LoadBalancer.Extensions
{
    public interface ICompressed
    {
        object Value { get; set; }
    }

    public sealed class Compressed<T> : ICompressed
    {
        public T Value { get; private set; }
        object ICompressed.Value { get => Value; set => Value = (T)value; }
        public Compressed(T value) { Value = value; }
        public override string ToString() => $"{Value}";
    }

    public static partial class Compression
    {
        private const int BufferLength = 1024;

        public static Compressed<T> AsCompressed<T>(this T value) => new Compressed<T>(value);

        public static byte[] CompressString(this string value)
        {
            using (var compressedStream = new MemoryStream())
            using (var compressor = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                var byteArray = Encoding.Unicode.GetBytes(value);
                compressor.Write(byteArray, 0, byteArray.Length);
                compressor.Flush();
                compressor.Close();
                return compressedStream.ToArray();
            }
        }

        public static string DecompressString(this byte[] value)
        {
            using (var compressedStream = new MemoryStream(value))
            {
                using (var inflator = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    using (var inflatedStream = new MemoryStream())
                    {
                        CopyTo(inflator, inflatedStream);
                        var inflatedArray = inflatedStream.ToArray();
                        return Encoding.Unicode.GetString(inflatedArray);
                    }
                }
            }
        }

        public static byte[] CompressArray(this byte[] value)
        {
            using (var compressedStream = new MemoryStream())
            using (var compressor = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                compressor.Write(value, 0, value.Length);
                compressor.Flush();
                compressor.Close();
                return compressedStream.ToArray();
            }
        }

        public static byte[] DecompressArray(this byte[] value)
        {
            using (var compressedStream = new MemoryStream(value))
            {
                using (var inflator = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    using (var inflatedStream = new MemoryStream())
                    {
                        CopyTo(inflator, inflatedStream);
                        return inflatedStream.ToArray();
                    }
                }
            }
        }

        private static void CopyTo(GZipStream inflator, MemoryStream inflatedStream)
        {
            var buffer = new byte[BufferLength];
            int bytesRead;
            while ((bytesRead = inflator.Read(buffer, 0, BufferLength)) > 0)
                inflatedStream.Write(buffer, 0, bytesRead);
            inflatedStream.Flush();
        }
    }
}
