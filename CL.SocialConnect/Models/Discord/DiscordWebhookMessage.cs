namespace CL.SocialConnect.Models.Discord;

/// <summary>
/// Represents a Discord webhook message
/// </summary>
public record DiscordWebhookMessage
{
    /// <summary>
    /// Gets the message content
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Gets the message username
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the message avatar URL
    /// </summary>
    public string? AvatarUrl { get; init; }

    /// <summary>
    /// Gets whether the message uses text-to-speech
    /// </summary>
    public bool TextToSpeech { get; init; }

    /// <summary>
    /// Gets the message embeds
    /// </summary>
    public IReadOnlyList<DiscordEmbed> Embeds { get; init; } = Array.Empty<DiscordEmbed>();

    /// <summary>
    /// Gets the allowed mentions
    /// </summary>
    public DiscordAllowedMentions? AllowedMentions { get; init; }

    /// <summary>
    /// Creates a simple text message
    /// </summary>
    public static DiscordWebhookMessage CreateTextMessage(string content) =>
        new() { Content = content };

    /// <summary>
    /// Creates a message with an embed
    /// </summary>
    public static DiscordWebhookMessage CreateEmbedMessage(DiscordEmbed embed, string? content = null) =>
        new() { Content = content, Embeds = new[] { embed } };

    /// <summary>
    /// Creates a message with multiple embeds
    /// </summary>
    public static DiscordWebhookMessage CreateEmbedMessage(IEnumerable<DiscordEmbed> embeds, string? content = null) =>
        new() { Content = content, Embeds = embeds.ToList().AsReadOnly() };
}

/// <summary>
/// Allowed mentions configuration for Discord messages
/// </summary>
public record DiscordAllowedMentions
{
    /// <summary>
    /// Gets the parse list (roles, users, everyone)
    /// </summary>
    public IReadOnlyList<string> Parse { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the user IDs to allow mentions for
    /// </summary>
    public IReadOnlyList<string> Users { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the role IDs to allow mentions for
    /// </summary>
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets whether to reply mention the user
    /// </summary>
    public bool RepliedUser { get; init; }
}

/// <summary>
/// OAuth2 token response from Discord
/// </summary>
public record DiscordOAuthToken
{
    /// <summary>
    /// Gets the access token
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets the token type (usually "Bearer")
    /// </summary>
    public required string TokenType { get; init; }

    /// <summary>
    /// Gets the token expiration time in seconds
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// Gets the refresh token (if available)
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Gets the scopes granted
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets when the token was issued
    /// </summary>
    public DateTime IssuedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets whether the token is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > IssuedAt.AddSeconds(ExpiresIn - 60); // 1 minute buffer
}
