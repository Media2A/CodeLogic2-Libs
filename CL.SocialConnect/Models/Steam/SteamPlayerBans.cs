namespace CL.SocialConnect.Models.Steam;

/// <summary>
/// Represents ban information for a Steam player
/// </summary>
public record SteamPlayerBans
{
    /// <summary>
    /// Gets the player's Steam ID
    /// </summary>
    public required string SteamId { get; init; }

    /// <summary>
    /// Gets whether the player has a community ban
    /// </summary>
    public bool CommunityBanned { get; init; }

    /// <summary>
    /// Gets whether the player has a VAC ban
    /// </summary>
    public bool VacBanned { get; init; }

    /// <summary>
    /// Gets the number of days since the last ban (or -1 if never banned)
    /// </summary>
    public int DaysSinceLastBan { get; init; }

    /// <summary>
    /// Gets the number of game bans
    /// </summary>
    public int NumberOfGameBans { get; init; }

    /// <summary>
    /// Gets the number of VAC bans
    /// </summary>
    public int NumberOfVacBans { get; init; }

    /// <summary>
    /// Gets the player's economy ban status (none, probation, banned)
    /// </summary>
    public string? EconomyBan { get; init; }

    /// <summary>
    /// Gets whether the player has any ban
    /// </summary>
    public bool HasBan => CommunityBanned || VacBanned || NumberOfGameBans > 0;

    /// <summary>
    /// Gets whether the player has a recent ban
    /// </summary>
    public bool HasRecentBan => DaysSinceLastBan >= 0 && DaysSinceLastBan <= 30;

    /// <summary>
    /// Gets a summary of ban information
    /// </summary>
    public override string ToString()
    {
        if (!HasBan)
            return "No bans";

        var parts = new List<string>();
        if (CommunityBanned)
            parts.Add("Community Ban");
        if (VacBanned)
            parts.Add($"VAC Ban (x{NumberOfVacBans})");
        if (NumberOfGameBans > 0)
            parts.Add($"Game Bans (x{NumberOfGameBans})");
        if (!string.IsNullOrEmpty(EconomyBan) && EconomyBan != "none")
            parts.Add($"Economy: {EconomyBan}");

        return string.Join(", ", parts);
    }
}
