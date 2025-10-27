namespace CL.SocialConnect.Models.Steam;

/// <summary>
/// Represents a Steam player profile
/// </summary>
public record SteamPlayer
{
    /// <summary>
    /// Gets the player's Steam ID (64-bit)
    /// </summary>
    public required string SteamId { get; init; }

    /// <summary>
    /// Gets the player's display name
    /// </summary>
    public required string PersonaName { get; init; }

    /// <summary>
    /// Gets the player's profile URL
    /// </summary>
    public required string ProfileUrl { get; init; }

    /// <summary>
    /// Gets the player's avatar URL (small)
    /// </summary>
    public required string Avatar { get; init; }

    /// <summary>
    /// Gets the player's avatar URL (medium)
    /// </summary>
    public required string AvatarMedium { get; init; }

    /// <summary>
    /// Gets the player's avatar URL (full)
    /// </summary>
    public required string AvatarFull { get; init; }

    /// <summary>
    /// Gets the player's persona state (0=offline, 1=online, 2=busy, 3=away, 4=snooze, 5=looking to trade, 6=looking to play)
    /// </summary>
    public int PersonaState { get; init; }

    /// <summary>
    /// Gets the last time the player updated their profile
    /// </summary>
    public DateTime? LastLogoff { get; init; }

    /// <summary>
    /// Gets the player's location city name
    /// </summary>
    public string? CityName { get; init; }

    /// <summary>
    /// Gets the player's location state code
    /// </summary>
    public string? StateCode { get; init; }

    /// <summary>
    /// Gets the player's location country code
    /// </summary>
    public string? CountryCode { get; init; }

    /// <summary>
    /// Gets the player's Steam level
    /// </summary>
    public int? SteamLevel { get; init; }

    /// <summary>
    /// Gets the player's primary group ID
    /// </summary>
    public string? PrimaryGroupId { get; init; }

    /// <summary>
    /// Gets the player's visibility state (1=Private, 2=Friends only, 3=Public)
    /// </summary>
    public int VisibilityState { get; init; }

    /// <summary>
    /// Gets whether the player's profile is public
    /// </summary>
    public bool IsPublic => VisibilityState == 3;

    /// <summary>
    /// Gets the player's real name (if public)
    /// </summary>
    public string? RealName { get; init; }

    /// <summary>
    /// Gets the creation timestamp of the account
    /// </summary>
    public DateTime? TimeCreated { get; init; }

    /// <summary>
    /// Gets the player's status string (online state)
    /// </summary>
    public string GetPersonaStateString() =>
        PersonaState switch
        {
            0 => "Offline",
            1 => "Online",
            2 => "Busy",
            3 => "Away",
            4 => "Snooze",
            5 => "Looking to Trade",
            6 => "Looking to Play",
            _ => "Unknown"
        };
}
