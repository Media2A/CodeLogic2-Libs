using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CL.Core.Utilities.StringNumeric;

/// <summary>
/// Provides string manipulation utilities
/// </summary>
public static partial class StringHelper
{
    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex SpecialCharsRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    /// <summary>
    /// Truncates string to specified length
    /// </summary>
    public static string Truncate(string input, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            return input;

        return input.Substring(0, maxLength) + suffix;
    }

    /// <summary>
    /// Capitalizes first character of string
    /// </summary>
    public static string CapitalizeFirst(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpper(input[0]) + input.Substring(1);
    }

    /// <summary>
    /// Converts string to title case
    /// </summary>
    public static string ToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
    }

    /// <summary>
    /// Removes special characters from string
    /// </summary>
    public static string RemoveSpecialCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return SpecialCharsRegex().Replace(input, string.Empty);
    }

    /// <summary>
    /// Normalizes whitespace (replaces multiple spaces with single space)
    /// </summary>
    public static string NormalizeWhitespace(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return WhitespaceRegex().Replace(input.Trim(), " ");
    }

    /// <summary>
    /// Reverses a string
    /// </summary>
    public static string Reverse(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        char[] chars = input.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    /// <summary>
    /// Counts occurrences of a substring
    /// </summary>
    public static int CountOccurrences(string input, string substring, bool caseSensitive = true)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(substring))
            return 0;

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int count = 0;
        int index = 0;

        while ((index = input.IndexOf(substring, index, comparison)) != -1)
        {
            count++;
            index += substring.Length;
        }

        return count;
    }

    /// <summary>
    /// Extracts text between two delimiters
    /// </summary>
    public static string? ExtractBetween(string input, string start, string end)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        int startIndex = input.IndexOf(start, StringComparison.Ordinal);
        if (startIndex == -1)
            return null;

        startIndex += start.Length;
        int endIndex = input.IndexOf(end, startIndex, StringComparison.Ordinal);

        if (endIndex == -1)
            return null;

        return input.Substring(startIndex, endIndex - startIndex);
    }

    /// <summary>
    /// Converts string to slug (URL-friendly)
    /// </summary>
    public static string ToSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        input = input.ToLowerInvariant().Trim();
        input = Regex.Replace(input, @"[^a-z0-9\s-]", string.Empty);
        input = Regex.Replace(input, @"\s+", "-");
        input = Regex.Replace(input, @"-+", "-");

        return input.Trim('-');
    }

    /// <summary>
    /// Masks a string (for sensitive data)
    /// </summary>
    public static string Mask(string input, int visibleStart = 0, int visibleEnd = 0, char maskChar = '*')
    {
        if (string.IsNullOrEmpty(input))
            return input;

        if (visibleStart + visibleEnd >= input.Length)
            return input;

        var result = new StringBuilder();

        if (visibleStart > 0)
            result.Append(input.Substring(0, visibleStart));

        int maskLength = input.Length - visibleStart - visibleEnd;
        result.Append(new string(maskChar, maskLength));

        if (visibleEnd > 0)
            result.Append(input.Substring(input.Length - visibleEnd));

        return result.ToString();
    }

    /// <summary>
    /// Word count
    /// </summary>
    public static int WordCount(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0;

        return input.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
