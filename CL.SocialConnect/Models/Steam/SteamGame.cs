namespace CL.SocialConnect.Models.Steam;

/// <summary>
/// Represents a Steam game in a player's library
/// </summary>
public record SteamGame
{
    /// <summary>
    /// Gets the game's app ID
    /// </summary>
    public required int AppId { get; init; }

    /// <summary>
    /// Gets the game name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the total playtime in minutes
    /// </summary>
    public int PlaytimeForever { get; init; }

    /// <summary>
    /// Gets the playtime in the last 2 weeks in minutes
    /// </summary>
    public int Playtime2Weeks { get; init; }

    /// <summary>
    /// Gets the icon URL
    /// </summary>
    public string? IconUrl { get; init; }

    /// <summary>
    /// Gets the logo URL
    /// </summary>
    public string? LogoUrl { get; init; }

    /// <summary>
    /// Gets whether the game is installed
    /// </summary>
    public bool HasCommunityVisibleStats { get; init; }

    /// <summary>
    /// Gets the total playtime in hours
    /// </summary>
    public double GetPlaytimeHours() => PlaytimeForever / 60.0;

    /// <summary>
    /// Gets the recent playtime in hours
    /// </summary>
    public double GetRecent2WeeksHours() => Playtime2Weeks / 60.0;

    /// <summary>
    /// Returns a string representation of the game with playtime
    /// </summary>
    public override string ToString() =>
        $"{Name} ({GetPlaytimeHours():F1}h total)";
}
