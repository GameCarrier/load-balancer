using LoadBalancer.Common;
using LoadBalancer.Extensions;

namespace LoadBalancer.Server.Common
{
    public static class AuthTokenUtils
    {
        public static CryptographySettings CryptographySettings { get; set; }

        public static string GenerateToken(string sessionId, KeyValueCollection claims)
        {
            var currentDateTime = DateTime.UtcNow;

            var data = Serialization.BinarySerialize(writer =>
            {
                writer.WriteStringASCII(sessionId);
                writer.WriteDictionary(claims);
                writer.WriteDateTime(currentDateTime);
            });

            return Cryptography.Encrypt(data, CryptographySettings.TokenSalt, CryptographySettings.SharedSecret);
        }

        public static bool ValidateToken(string token, out AuthContext context)
        {
            context = null;

            byte[] data;

            try
            {
                data = Cryptography.DecryptByteArray(token, CryptographySettings.TokenSalt, CryptographySettings.SharedSecret);
            }
            catch (Exception)
            {
                return false;
            }

            string sessionId = null;
            KeyValueCollection claims = null;
            DateTime loginDate = DateTime.MinValue;

            Serialization.BinaryDeserialize(data, reader =>
            {
                sessionId = reader.ReadStringASCII();
                claims = reader.ReadDictionary();
                loginDate = reader.ReadDateTime();
            });

            if (Math.Abs(DateTime.UtcNow.Subtract(loginDate).TotalMinutes) > CryptographySettings.TokenTtl)
                return false;

            context = new AuthContext { SessionId = sessionId, Claims = claims, LoginDate = loginDate };
            return true;
        }
    }
}
