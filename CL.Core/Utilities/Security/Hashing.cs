using System.Security.Cryptography;
using System.Text;

namespace CL.Core.Utilities.Security;

/// <summary>
/// Provides cryptographic hashing utilities
/// </summary>
public static class Hashing
{
    /// <summary>
    /// Computes SHA-256 hash of input string
    /// </summary>
    public static string Sha256(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(inputBytes);
        return BytesToHex(hashBytes);
    }

    /// <summary>
    /// Computes SHA-512 hash of input string
    /// </summary>
    public static string Sha512(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA512.HashData(inputBytes);
        return BytesToHex(hashBytes);
    }

    /// <summary>
    /// Computes MD5 hash of input string (use only for legacy compatibility)
    /// </summary>
    [Obsolete("MD5 is cryptographically broken. Use SHA-256 or SHA-512 instead.")]
    public static string Md5(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = MD5.HashData(inputBytes);
        return BytesToHex(hashBytes);
    }

    /// <summary>
    /// Hashes a password with PBKDF2 and returns hash with salt
    /// </summary>
    public static string HashPassword(string password, int iterations = 100000)
    {
        // Generate a random salt
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        // Hash the password
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            32);

        // Combine salt and hash
        byte[] hashBytes = new byte[48]; // 16 bytes salt + 32 bytes hash
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        // Return as base64 string
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Verifies a password against a hashed password
    /// </summary>
    public static bool VerifyPassword(string password, string hashedPassword, int iterations = 100000)
    {
        try
        {
            // Extract the bytes
            byte[] hashBytes = Convert.FromBase64String(hashedPassword);

            // Extract salt (first 16 bytes)
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);

            // Compute hash of input password
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                32);

            // Compare hash (bytes 16-47) with computed hash
            for (int i = 0; i < 32; i++)
            {
                if (hashBytes[i + 16] != hash[i])
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a cryptographically secure random salt
    /// </summary>
    public static string GenerateSalt(int size = 16)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(size);
        return Convert.ToBase64String(salt);
    }

    /// <summary>
    /// Computes HMAC-SHA256 of input with key
    /// </summary>
    public static string HmacSha256(string input, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);

        using var hmac = new HMACSHA256(keyBytes);
        byte[] hashBytes = hmac.ComputeHash(inputBytes);
        return BytesToHex(hashBytes);
    }

    /// <summary>
    /// Computes HMAC-SHA512 of input with key
    /// </summary>
    public static string HmacSha512(string input, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);

        using var hmac = new HMACSHA512(keyBytes);
        byte[] hashBytes = hmac.ComputeHash(inputBytes);
        return BytesToHex(hashBytes);
    }

    /// <summary>
    /// Converts byte array to hexadecimal string
    /// </summary>
    private static string BytesToHex(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
