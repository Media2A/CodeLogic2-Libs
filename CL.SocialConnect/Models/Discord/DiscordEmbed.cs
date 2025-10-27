namespace CL.SocialConnect.Models.Discord;

/// <summary>
/// Represents a Discord embed (rich message)
/// </summary>
public record DiscordEmbed
{
    /// <summary>
    /// Gets the embed title
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the embed description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the embed color (decimal format)
    /// </summary>
    public int? Color { get; init; }

    /// <summary>
    /// Gets the embed fields
    /// </summary>
    public IReadOnlyList<DiscordEmbedField> Fields { get; init; } = Array.Empty<DiscordEmbedField>();

    /// <summary>
    /// Gets the embed thumbnail
    /// </summary>
    public DiscordEmbedImage? Thumbnail { get; init; }

    /// <summary>
    /// Gets the embed image
    /// </summary>
    public DiscordEmbedImage? Image { get; init; }

    /// <summary>
    /// Gets the embed author
    /// </summary>
    public DiscordEmbedAuthor? Author { get; init; }

    /// <summary>
    /// Gets the embed footer
    /// </summary>
    public DiscordEmbedFooter? Footer { get; init; }

    /// <summary>
    /// Gets the embed URL
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Gets when the embed was last updated
    /// </summary>
    public DateTime? Timestamp { get; init; }
}

/// <summary>
/// Represents a field in a Discord embed
/// </summary>
public record DiscordEmbedField
{
    /// <summary>
    /// Gets the field name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the field value
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets whether the field is displayed inline
    /// </summary>
    public bool Inline { get; init; }
}

/// <summary>
/// Represents an image in a Discord embed
/// </summary>
public record DiscordEmbedImage
{
    /// <summary>
    /// Gets the image URL
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the image width
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Gets the image height
    /// </summary>
    public int? Height { get; init; }
}

/// <summary>
/// Represents an author in a Discord embed
/// </summary>
public record DiscordEmbedAuthor
{
    /// <summary>
    /// Gets the author name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the author URL
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Gets the author icon URL
    /// </summary>
    public string? IconUrl { get; init; }
}

/// <summary>
/// Represents a footer in a Discord embed
/// </summary>
public record DiscordEmbedFooter
{
    /// <summary>
    /// Gets the footer text
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Gets the footer icon URL
    /// </summary>
    public string? IconUrl { get; init; }
}
