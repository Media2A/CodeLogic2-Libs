namespace CL.SocialConnect.Models.Steam;

/// <summary>
/// Result of Steam authentication/validation
/// </summary>
public record SteamAuthResult
{
    /// <summary>
    /// Gets the validated Steam ID (64-bit)
    /// </summary>
    public required string SteamId { get; init; }

    /// <summary>
    /// Gets the Steam ID in 32-bit format
    /// </summary>
    public required string SteamId32 { get; init; }

    /// <summary>
    /// Gets whether the authentication was successful
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message if authentication failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets when the authentication occurred
    /// </summary>
    public DateTime AuthenticatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Converts a 64-bit Steam ID to 32-bit format
    /// </summary>
    public static string ConvertTo32Bit(string steamId64)
    {
        if (!ulong.TryParse(steamId64, out var id64))
            throw new ArgumentException("Invalid Steam ID 64", nameof(steamId64));

        var accountNumber = id64 & 0xFFFFFFFF;
        return accountNumber.ToString();
    }

    /// <summary>
    /// Converts a 32-bit Steam ID to 64-bit format
    /// </summary>
    public static string ConvertTo64Bit(string steamId32)
    {
        if (!uint.TryParse(steamId32, out var id32))
            throw new ArgumentException("Invalid Steam ID 32", nameof(steamId32));

        const ulong steamIdBase = 0x0110000100000000;
        var id64 = steamIdBase + id32;
        return id64.ToString();
    }

    /// <summary>
    /// Creates a successful authentication result
    /// </summary>
    public static SteamAuthResult Success(string steamId64)
    {
        var id32 = ConvertTo32Bit(steamId64);
        return new SteamAuthResult
        {
            SteamId = steamId64,
            SteamId32 = id32,
            IsSuccess = true
        };
    }

    /// <summary>
    /// Creates a failed authentication result
    /// </summary>
    public static SteamAuthResult Failure(string errorMessage) =>
        new()
        {
            SteamId = string.Empty,
            SteamId32 = string.Empty,
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
}
