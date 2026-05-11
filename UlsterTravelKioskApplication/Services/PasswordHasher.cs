using System;
using System.Security.Cryptography; // provides cryptographic services

namespace UlsterTravelKioskApplication.Services
{
    // helper class for hashing passwords
    public static class PasswordHasher
    {
        private const int SaltSize = 16;      // size of the salt in bytes
        private const int KeySize = 32;       // size of the key in bytes
        private const int Iterations = 100_000; // number of PBKDF2 iterations

        // method for hashing a password
        public static string Hash(string password)
        {
            password ??= ""; // replaces null values with empty strings

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize); // provides random salt

            // creates PBKDF2 key object
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256);

            byte[] key = pbkdf2.GetBytes(KeySize); // derives key from password

            return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        }

        // Method for verifyng password against hash
        public static bool Verify(string password, string stored)
        {
            // replaces null password and username with empty string
            password ??= "";
            stored ??= "";
            stored = stored.Trim().Trim('"'); // removes any blank spaces and/or quotes

            // validates hash prefix
            if (!stored.StartsWith("PBKDF2$", StringComparison.Ordinal))
                return false;

            var parts = stored.Split('$'); // splits hash into components
            if (parts.Length != 4) return false; // ensures correct number of parts

            if (!int.TryParse(parts[1], out int iterations)) return false; // parses iteration count

            if (!TryBase64(parts[2], out byte[] salt)) return false; // decodes salt
            if (!TryBase64(parts[3], out byte[] expected)) return false; // decodes hash

            // error handling
            try
            {
                // recreates the PBKDF2 object
                using var pbkdf2 = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256);

                byte[] actual = pbkdf2.GetBytes(expected.Length); // calculates hash

                return CryptographicOperations.FixedTimeEquals(actual, expected); // compares hash
            }
            catch
            {
                return false; // returns false if verification fails
            }
        }

        // method for decoding Base64 strings
        private static bool TryBase64(string input, out byte[] bytes)
        {
            bytes = Array.Empty<byte>(); // initalizzes output byte array
            input = (input ?? "").Trim().Trim('"');

            try
            {
                bytes = Convert.FromBase64String(input); // converts Base64 string into bytes
                return bytes.Length > 0; // ensures that the data exists
            }
            catch
            {
                return false; // returns false if conversion is unsuccessful
            }
        }
    }
}
