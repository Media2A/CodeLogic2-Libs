namespace CL.SocialConnect.Models.Discord;

/// <summary>
/// Represents a Discord user
/// </summary>
public record DiscordUser
{
    /// <summary>
    /// Gets the unique user ID
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the username
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets the user's discriminator (legacy, deprecated by Discord)
    /// </summary>
    public string? Discriminator { get; init; }

    /// <summary>
    /// Gets the user's global name
    /// </summary>
    public string? GlobalName { get; init; }

    /// <summary>
    /// Gets the user's avatar hash
    /// </summary>
    public string? Avatar { get; init; }

    /// <summary>
    /// Gets the user's email address
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets whether the user's email is verified
    /// </summary>
    public bool? Verified { get; init; }

    /// <summary>
    /// Gets whether the user is a bot
    /// </summary>
    public bool IsBot { get; init; }

    /// <summary>
    /// Gets the user's banner color
    /// </summary>
    public string? BannerColor { get; init; }

    /// <summary>
    /// Gets the user's accent color
    /// </summary>
    public int? AccentColor { get; init; }

    /// <summary>
    /// Gets the user's public profile
    /// </summary>
    public bool PublicProfile { get; init; }

    /// <summary>
    /// Gets the user's locale
    /// </summary>
    public string? Locale { get; init; }

    /// <summary>
    /// Returns the full username (username#discriminator or just username)
    /// </summary>
    public override string ToString() =>
        string.IsNullOrEmpty(Discriminator) || Discriminator == "0"
            ? Username
            : $"{Username}#{Discriminator}";

    /// <summary>
    /// Gets the CDN URL for the user's avatar
    /// </summary>
    public string GetAvatarUrl(int size = 256) =>
        string.IsNullOrEmpty(Avatar)
            ? $"https://cdn.discordapp.com/embed/avatars/{int.Parse(Id) % 5}.png"
            : $"https://cdn.discordapp.com/avatars/{Id}/{Avatar}.png?size={size}";
}
