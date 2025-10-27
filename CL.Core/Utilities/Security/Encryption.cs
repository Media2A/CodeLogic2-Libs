using System.Security.Cryptography;
using System.Text;

namespace CL.Core.Utilities.Security;

/// <summary>
/// Provides encryption and decryption utilities using AES
/// </summary>
public static class Encryption
{
    /// <summary>
    /// Encrypts text using AES encryption with a password
    /// </summary>
    public static string EncryptAes(string plainText, string password)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));

        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        byte[] clearBytes = Encoding.UTF8.GetBytes(plainText);

        // Generate a random salt
        byte[] salt = RandomNumberGenerator.GetBytes(16);

        // Derive key from password using PBKDF2
        using var keyDerivation = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        byte[] key = keyDerivation.GetBytes(32); // 256-bit key

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        byte[] iv = aes.IV;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();

        // Write salt and IV first
        ms.Write(salt, 0, salt.Length);
        ms.Write(iv, 0, iv.Length);

        // Write encrypted data
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(clearBytes, 0, clearBytes.Length);
            cs.FlushFinalBlock();
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Decrypts AES-encrypted text using a password
    /// </summary>
    public static string DecryptAes(string cipherText, string password)
    {
        if (string.IsNullOrEmpty(cipherText))
            throw new ArgumentException("Cipher text cannot be null or empty", nameof(cipherText));

        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        byte[] cipherBytes = Convert.FromBase64String(cipherText);

        using var ms = new MemoryStream(cipherBytes);

        // Read salt
        byte[] salt = new byte[16];
        ms.Read(salt, 0, salt.Length);

        // Read IV
        byte[] iv = new byte[16];
        ms.Read(iv, 0, iv.Length);

        // Derive key from password
        using var keyDerivation = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        byte[] key = keyDerivation.GetBytes(32);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var resultStream = new MemoryStream();

        cs.CopyTo(resultStream);
        return Encoding.UTF8.GetString(resultStream.ToArray());
    }

    /// <summary>
    /// Encrypts bytes using AES encryption
    /// </summary>
    public static byte[] EncryptBytes(byte[] data, string password)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(16);

        using var keyDerivation = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        byte[] key = keyDerivation.GetBytes(32);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        byte[] iv = aes.IV;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();

        ms.Write(salt, 0, salt.Length);
        ms.Write(iv, 0, iv.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Decrypts bytes using AES decryption
    /// </summary>
    public static byte[] DecryptBytes(byte[] encryptedData, string password)
    {
        if (encryptedData == null || encryptedData.Length == 0)
            throw new ArgumentException("Encrypted data cannot be null or empty", nameof(encryptedData));

        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        using var ms = new MemoryStream(encryptedData);

        byte[] salt = new byte[16];
        ms.Read(salt, 0, salt.Length);

        byte[] iv = new byte[16];
        ms.Read(iv, 0, iv.Length);

        using var keyDerivation = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        byte[] key = keyDerivation.GetBytes(32);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var resultStream = new MemoryStream();

        cs.CopyTo(resultStream);
        return resultStream.ToArray();
    }

    /// <summary>
    /// Generates a random encryption key
    /// </summary>
    public static string GenerateKey(int length = 32)
    {
        byte[] key = RandomNumberGenerator.GetBytes(length);
        return Convert.ToBase64String(key);
    }
}
