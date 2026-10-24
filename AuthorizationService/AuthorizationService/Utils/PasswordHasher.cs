using System;
using System.Security.Cryptography;

namespace AuthorizationService.Utils
{
    // Simple PBKDF2-based password hasher for demonstration.
    public static class PasswordHasher
    {
        private const int Iterations = 100_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        // Returns base64(salt|key)
        public static string Hash(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = derive.GetBytes(KeySize);

            var result = new byte[SaltSize + KeySize];
            Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
            Buffer.BlockCopy(key, 0, result, SaltSize, KeySize);

            return Convert.ToBase64String(result);
        }

        public static bool Verify(string password, string stored)
        {
            try
            {
                var bytes = Convert.FromBase64String(stored);
                var salt = new byte[SaltSize];
                Buffer.BlockCopy(bytes, 0, salt, 0, SaltSize);
                using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                var key = derive.GetBytes(KeySize);
                for (int i = 0; i < KeySize; i++)
                {
                    if (bytes[SaltSize + i] != key[i]) return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
