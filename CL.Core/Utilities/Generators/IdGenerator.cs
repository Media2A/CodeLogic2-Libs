using System.Security.Cryptography;

namespace CL.Core.Utilities.Generators;

/// <summary>
/// Provides methods for generating various types of unique identifiers
/// </summary>
public static class IdGenerator
{
    private static int _sequentialCounter = 0;
    private static readonly object _counterLock = new();

    /// <summary>
    /// Generates a new GUID
    /// </summary>
    public static string NewGuid() => Guid.NewGuid().ToString();

    /// <summary>
    /// Generates a new GUID without hyphens
    /// </summary>
    public static string NewGuidNoDashes() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// Generates a sequential ID (thread-safe)
    /// </summary>
    public static long Sequential()
    {
        return Interlocked.Increment(ref _sequentialCounter);
    }

    /// <summary>
    /// Generates a timestamp-based ID using DateTime ticks
    /// </summary>
    public static string Timestamp() => DateTime.UtcNow.Ticks.ToString();

    /// <summary>
    /// Generates a timestamp ID with a prefix
    /// </summary>
    public static string TimestampWithPrefix(string prefix) => $"{prefix}_{DateTime.UtcNow.Ticks}";

    /// <summary>
    /// Generates a cryptographically secure random ID
    /// </summary>
    public static string Random(int length = 16, bool alphanumeric = true)
    {
        const string alphanumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        const string allChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-_=+[]{}|;:,.<>?";

        string chars = alphanumeric ? alphanumericChars : allChars;
        return RandomString(length, chars);
    }

    /// <summary>
    /// Generates a random hex string
    /// </summary>
    public static string RandomHex(int byteCount = 16)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Generates a base64-encoded random string
    /// </summary>
    public static string RandomBase64(int byteCount = 16)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToBase64String(bytes).TrimEnd('=');
    }

    /// <summary>
    /// Generates a URL-safe random ID
    /// </summary>
    public static string UrlSafe(int length = 16)
    {
        const string urlSafeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        return RandomString(length, urlSafeChars);
    }

    /// <summary>
    /// Generates a Nano ID (compact, URL-safe, unique)
    /// </summary>
    public static string NanoId(int length = 21)
    {
        const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        return RandomString(length, alphabet);
    }

    /// <summary>
    /// Generates a sortable ID combining timestamp and random component
    /// </summary>
    public static string Sortable()
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string randomPart = RandomHex(8);
        return $"{timestamp:x16}{randomPart}";
    }

    /// <summary>
    /// Helper method to generate random string from character set
    /// </summary>
    private static string RandomString(int length, string chars)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be positive", nameof(length));

        if (string.IsNullOrEmpty(chars))
            throw new ArgumentException("Character set cannot be empty", nameof(chars));

        Span<char> result = stackalloc char[length];
        Span<byte> randomBytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(randomBytes);

        for (int i = 0; i < length; i++)
        {
            result[i] = chars[randomBytes[i] % chars.Length];
        }

        return new string(result);
    }
}
