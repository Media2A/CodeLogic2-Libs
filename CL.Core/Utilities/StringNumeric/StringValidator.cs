using System.Text.RegularExpressions;

namespace CL.Core.Utilities.StringNumeric;

/// <summary>
/// Provides string validation utilities
/// </summary>
public static partial class StringValidator
{
    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[a-zA-Z]+$")]
    private static partial Regex LettersOnlyRegex();

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnlyRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9]+$")]
    private static partial Regex AlphanumericRegex();

    [GeneratedRegex(@"^https?://")]
    private static partial Regex UrlRegex();

    /// <summary>
    /// Validates if string is a valid email address
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return EmailRegex().IsMatch(email);
    }

    /// <summary>
    /// Checks if string contains only letters
    /// </summary>
    public static bool IsAllLetters(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return LettersOnlyRegex().IsMatch(input);
    }

    /// <summary>
    /// Checks if string contains only digits
    /// </summary>
    public static bool IsAllDigits(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return DigitsOnlyRegex().IsMatch(input);
    }

    /// <summary>
    /// Checks if string is alphanumeric only
    /// </summary>
    public static bool IsAlphanumeric(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return AlphanumericRegex().IsMatch(input);
    }

    /// <summary>
    /// Checks if string is a valid URL
    /// </summary>
    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return UrlRegex().IsMatch(url) && Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    /// <summary>
    /// Checks if string is null or whitespace
    /// </summary>
    public static bool IsNullOrWhiteSpace(string? input) => string.IsNullOrWhiteSpace(input);

    /// <summary>
    /// Checks if string is null or empty
    /// </summary>
    public static bool IsNullOrEmpty(string? input) => string.IsNullOrEmpty(input);

    /// <summary>
    /// Validates if string length is within range
    /// </summary>
    public static bool IsLengthInRange(string input, int minLength, int maxLength)
    {
        if (string.IsNullOrEmpty(input))
            return minLength == 0;

        return input.Length >= minLength && input.Length <= maxLength;
    }
}
