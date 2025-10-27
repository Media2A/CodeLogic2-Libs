using System.Security.Cryptography;
using System.Text;

namespace CL.Core.Utilities.Generators;

/// <summary>
/// Provides methods for generating secure passwords
/// </summary>
public static class PasswordGenerator
{
    private const string LowercaseChars = "abcdefghijkmnopqrstuvwxyz";
    private const string UppercaseChars = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
    private const string DigitChars = "0123456789";
    private const string SpecialChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";
    private const string SimpleSpecialChars = "!@$?_-";

    /// <summary>
    /// Generates a random password with specified options
    /// </summary>
    public static string Generate(int length = 16, bool includeUppercase = true, bool includeDigits = true, bool includeSpecialChars = true)
    {
        if (length < 4)
            throw new ArgumentException("Password length must be at least 4 characters", nameof(length));

        var charset = LowercaseChars;
        if (includeUppercase) charset += UppercaseChars;
        if (includeDigits) charset += DigitChars;
        if (includeSpecialChars) charset += SimpleSpecialChars;

        return GenerateFromCharset(length, charset);
    }

    /// <summary>
    /// Generates a strong password with all character types
    /// </summary>
    public static string GenerateStrong(int length = 16)
    {
        if (length < 8)
            throw new ArgumentException("Strong password length must be at least 8 characters", nameof(length));

        var password = new StringBuilder(length);

        // Ensure at least one of each type
        password.Append(GetRandomChar(LowercaseChars));
        password.Append(GetRandomChar(UppercaseChars));
        password.Append(GetRandomChar(DigitChars));
        password.Append(GetRandomChar(SpecialChars));

        // Fill the rest randomly
        var allChars = LowercaseChars + UppercaseChars + DigitChars + SpecialChars;
        for (int i = 4; i < length; i++)
        {
            password.Append(GetRandomChar(allChars));
        }

        // Shuffle the password
        return Shuffle(password.ToString());
    }

    /// <summary>
    /// Generates a memorable password using words (passphrase)
    /// </summary>
    public static string GeneratePassphrase(int wordCount = 4, string separator = "-")
    {
        // Simple word list for demonstration - in production, use a larger dictionary
        string[] words = new[]
        {
            "apple", "beach", "cloud", "dance", "earth", "flame", "grass", "house",
            "island", "jungle", "knight", "lemon", "mountain", "night", "ocean", "planet",
            "queen", "river", "stone", "thunder", "universe", "valley", "water", "xenon",
            "yellow", "zenith", "azure", "bronze", "crimson", "diamond", "emerald", "falcon"
        };

        var selectedWords = new List<string>();
        for (int i = 0; i < wordCount; i++)
        {
            int index = RandomNumberGenerator.GetInt32(words.Length);
            selectedWords.Add(Capitalize(words[index]));
        }

        // Add a random number at the end
        int number = RandomNumberGenerator.GetInt32(100, 999);
        selectedWords.Add(number.ToString());

        return string.Join(separator, selectedWords);
    }

    /// <summary>
    /// Generates a PIN code
    /// </summary>
    public static string GeneratePin(int length = 4)
    {
        if (length < 4 || length > 10)
            throw new ArgumentException("PIN length must be between 4 and 10", nameof(length));

        return GenerateFromCharset(length, DigitChars);
    }

    /// <summary>
    /// Validates password strength
    /// </summary>
    public static PasswordStrength CalculateStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return PasswordStrength.VeryWeak;

        int score = 0;

        // Length
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;
        if (password.Length >= 16) score++;

        // Character types
        if (password.Any(char.IsLower)) score++;
        if (password.Any(char.IsUpper)) score++;
        if (password.Any(char.IsDigit)) score++;
        if (password.Any(ch => SpecialChars.Contains(ch))) score++;

        // Variety
        if (password.Distinct().Count() >= password.Length * 0.7) score++;

        return score switch
        {
            >= 8 => PasswordStrength.VeryStrong,
            >= 6 => PasswordStrength.Strong,
            >= 4 => PasswordStrength.Medium,
            >= 2 => PasswordStrength.Weak,
            _ => PasswordStrength.VeryWeak
        };
    }

    private static string GenerateFromCharset(int length, string charset)
    {
        Span<char> result = stackalloc char[length];
        Span<byte> randomBytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(randomBytes);

        for (int i = 0; i < length; i++)
        {
            result[i] = charset[randomBytes[i] % charset.Length];
        }

        return new string(result);
    }

    private static char GetRandomChar(string charset)
    {
        int index = RandomNumberGenerator.GetInt32(charset.Length);
        return charset[index];
    }

    private static string Shuffle(string input)
    {
        var chars = input.ToCharArray();
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    private static string Capitalize(string word)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        return char.ToUpper(word[0]) + word.Substring(1).ToLower();
    }
}

/// <summary>
/// Password strength levels
/// </summary>
public enum PasswordStrength
{
    VeryWeak = 0,
    Weak = 1,
    Medium = 2,
    Strong = 3,
    VeryStrong = 4
}
