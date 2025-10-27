namespace CL.Core.Utilities.TimeDate;

/// <summary>
/// Provides date and time utilities
/// </summary>
public static class DateTimeHelper
{
    /// <summary>
    /// Gets Unix timestamp (seconds since epoch)
    /// </summary>
    public static long GetUnixTimestamp() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>
    /// Gets Unix timestamp in milliseconds
    /// </summary>
    public static long GetUnixTimestampMilliseconds() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// Converts Unix timestamp to DateTime
    /// </summary>
    public static DateTime FromUnixTimestamp(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
    }

    /// <summary>
    /// Converts Unix timestamp (milliseconds) to DateTime
    /// </summary>
    public static DateTime FromUnixTimestampMilliseconds(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
    }

    /// <summary>
    /// Gets relative time description (e.g., "2 hours ago")
    /// </summary>
    public static string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime.ToUniversalTime();

        if (span.TotalSeconds < 60)
            return "just now";

        if (span.TotalMinutes < 60)
            return $"{(int)span.TotalMinutes} minute{(span.TotalMinutes >= 2 ? "s" : "")} ago";

        if (span.TotalHours < 24)
            return $"{(int)span.TotalHours} hour{(span.TotalHours >= 2 ? "s" : "")} ago";

        if (span.TotalDays < 30)
            return $"{(int)span.TotalDays} day{(span.TotalDays >= 2 ? "s" : "")} ago";

        if (span.TotalDays < 365)
            return $"{(int)(span.TotalDays / 30)} month{(span.TotalDays / 30 >= 2 ? "s" : "")} ago";

        return $"{(int)(span.TotalDays / 365)} year{(span.TotalDays / 365 >= 2 ? "s" : "")} ago";
    }

    /// <summary>
    /// Checks if date is in the past
    /// </summary>
    public static bool IsInPast(DateTime dateTime) => dateTime < DateTime.UtcNow;

    /// <summary>
    /// Checks if date is in the future
    /// </summary>
    public static bool IsInFuture(DateTime dateTime) => dateTime > DateTime.UtcNow;

    /// <summary>
    /// Checks if date is today
    /// </summary>
    public static bool IsToday(DateTime dateTime) => dateTime.Date == DateTime.UtcNow.Date;

    /// <summary>
    /// Gets start of day
    /// </summary>
    public static DateTime StartOfDay(DateTime dateTime) => dateTime.Date;

    /// <summary>
    /// Gets end of day
    /// </summary>
    public static DateTime EndOfDay(DateTime dateTime) => dateTime.Date.AddDays(1).AddTicks(-1);

    /// <summary>
    /// Gets start of month
    /// </summary>
    public static DateTime StartOfMonth(DateTime dateTime) => new(dateTime.Year, dateTime.Month, 1);

    /// <summary>
    /// Gets end of month
    /// </summary>
    public static DateTime EndOfMonth(DateTime dateTime) =>
        new DateTime(dateTime.Year, dateTime.Month, 1).AddMonths(1).AddTicks(-1);

    /// <summary>
    /// Formats DateTime in ISO 8601 format
    /// </summary>
    public static string ToIso8601(DateTime dateTime) => dateTime.ToString("O");

    /// <summary>
    /// Parses ISO 8601 format
    /// </summary>
    public static DateTime ParseIso8601(string dateString) => DateTime.Parse(dateString, null, System.Globalization.DateTimeStyles.RoundtripKind);

    /// <summary>
    /// Calculates age from birth date
    /// </summary>
    public static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;

        if (birthDate.Date > today.AddYears(-age))
            age--;

        return age;
    }

    /// <summary>
    /// Checks if two date ranges overlap
    /// </summary>
    public static bool DoRangesOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
    {
        return start1 <= end2 && start2 <= end1;
    }
}
