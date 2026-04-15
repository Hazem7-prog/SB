using SB.Interfaces;
using System.Security.Cryptography;

namespace SB.Services
{
    public class OtpService : IOtpService
    {
        private const int OtpLength = 4;
        private const int MinOtpValue = 1000;
        private const int MaxOtpValue = 9999;
        private const int IterationCount = 10000;
        private const int KeySize = 32; // 256 bits

        /// <summary>
        /// Generates a 4-digit numeric OTP code (1000-9999).
        /// </summary>
        public string GenerateOtp()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] buffer = new byte[4];
                rng.GetBytes(buffer);
                int randomNumber = Math.Abs(BitConverter.ToInt32(buffer, 0));
                int otp = (randomNumber % (MaxOtpValue - MinOtpValue + 1)) + MinOtpValue;
                return otp.ToString();
            }
        }

        /// <summary>
        /// Hashes an OTP code using PBKDF2 with SHA256.
        /// </summary>
        public string HashOtp(string otp)
        {
            if (string.IsNullOrWhiteSpace(otp))
                throw new ArgumentException("OTP cannot be null or empty.", nameof(otp));

            // Generate a random salt
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] salt = new byte[16];
                rng.GetBytes(salt);

                // Hash the OTP with the salt using PBKDF2-SHA256
                using (var pbkdf2 = new Rfc2898DeriveBytes(otp, salt, IterationCount, HashAlgorithmName.SHA256))
                {
                    byte[] hash = pbkdf2.GetBytes(KeySize);

                    // Combine salt and hash for storage
                    byte[] hashWithSalt = new byte[salt.Length + hash.Length];
                    Array.Copy(salt, 0, hashWithSalt, 0, salt.Length);
                    Array.Copy(hash, 0, hashWithSalt, salt.Length, hash.Length);

                    // Return as Base64 for database storage
                    return Convert.ToBase64String(hashWithSalt);
                }
            }
        }

        /// <summary>
        /// Verifies a plain OTP against its hash.
        /// </summary>
        public bool VerifyOtp(string otp, string hash)
        {
            if (string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(hash))
                return false;

            try
            {
                byte[] hashWithSalt = Convert.FromBase64String(hash);

                // Extract salt (first 16 bytes)
                byte[] salt = new byte[16];
                Array.Copy(hashWithSalt, 0, salt, 0, salt.Length);

                // Extract stored hash (remaining bytes)
                byte[] storedHash = new byte[hashWithSalt.Length - salt.Length];
                Array.Copy(hashWithSalt, salt.Length, storedHash, 0, storedHash.Length);

                // Hash the provided OTP with the same salt
                using (var pbkdf2 = new Rfc2898DeriveBytes(otp, salt, IterationCount, HashAlgorithmName.SHA256))
                {
                    byte[] computedHash = pbkdf2.GetBytes(KeySize);

                    // Compare hashes using constant-time comparison to prevent timing attacks
                    return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
                }
            }
            catch
            {
                // Return false if hash is invalid format
                return false;
            }
        }
    }
}
