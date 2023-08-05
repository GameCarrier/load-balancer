using LoadBalancer.Extensions;
using System.Security.Cryptography;
using System.Text;

namespace LoadBalancer.Server.Common
{
    public static class Cryptography
    {
        public static string Encrypt(byte[] data, string salt, string sharedSecret)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            return Encrypt(stream =>
            {
                using (var writer = new BinaryWriter(stream))
                    writer.WriteByteArray(data);
            }, salt, sharedSecret);
        }

        public static string Encrypt(Action<Stream> writeAction, string salt, string sharedSecret)
        {
            if (writeAction == null)
                throw new ArgumentNullException("writeAction");
            if (string.IsNullOrEmpty(salt))
                throw new ArgumentNullException("salt");
            if (string.IsNullOrEmpty(sharedSecret))
                throw new ArgumentNullException("sharedSecret");

            Aes aesAlg = null;
            var saltBytes = Encoding.ASCII.GetBytes(salt);

            try
            {
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, saltBytes);

                aesAlg = Aes.Create();
                aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        writeAction(csEncrypt);

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
            finally
            {
                if (aesAlg != null)
                    aesAlg.Clear();
            }
        }

        public static byte[] DecryptByteArray(string encodedPhrase, string salt, string sharedSecret)
        {
            byte[] data = null;
            Decrypt(encodedPhrase, salt, sharedSecret, stream =>
            {
                using (var reader = new BinaryReader(stream))
                    data = reader.ReadByteArray();
            });
            return data;
        }

        public static void Decrypt(string encodedPhrase, string salt, string sharedSecret, Action<Stream> readAction)
        {
            if (string.IsNullOrEmpty(encodedPhrase))
                throw new ArgumentNullException("encodedPhrase");
            if (string.IsNullOrEmpty(salt))
                throw new ArgumentNullException("salt");
            if (string.IsNullOrEmpty(sharedSecret))
                throw new ArgumentNullException("sharedSecret");
            if (readAction == null)
                throw new ArgumentNullException("readAction");

            Aes aesAlg = null;
            var saltBytes = Encoding.ASCII.GetBytes(salt);

            try
            {
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, saltBytes);

                byte[] bytes = Convert.FromBase64String(encodedPhrase);
                using (MemoryStream msDecrypt = new MemoryStream(bytes))
                {
                    aesAlg = Aes.Create();
                    aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                    aesAlg.IV = ReadByteArray(msDecrypt);

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        readAction(csDecrypt);
                }
            }
            finally
            {
                if (aesAlg != null)
                    aesAlg.Clear();
            }
        }

        private static byte[] ReadByteArray(Stream s)
        {
            byte[] rawLength = new byte[sizeof(int)];
            if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
            {
                throw new SystemException("Stream did not contain properly formatted byte array");
            }

            byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
            if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new SystemException("Did not read byte array properly");
            }

            return buffer;
        }
    }
}
