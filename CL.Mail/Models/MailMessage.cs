namespace CL.Mail.Models;

/// <summary>
/// Represents an email message
/// </summary>
public record MailMessage
{
    /// <summary>
    /// Gets the sender email address
    /// </summary>
    public required string From { get; init; }

    /// <summary>
    /// Gets the optional sender display name
    /// </summary>
    public string? FromName { get; init; }

    /// <summary>
    /// Gets the recipient addresses
    /// </summary>
    public required IReadOnlyList<string> To { get; init; }

    /// <summary>
    /// Gets the CC recipient addresses
    /// </summary>
    public IReadOnlyList<string> Cc { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the BCC recipient addresses
    /// </summary>
    public IReadOnlyList<string> Bcc { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the email subject
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Gets the plain text body
    /// </summary>
    public string? TextBody { get; init; }

    /// <summary>
    /// Gets the HTML body
    /// </summary>
    public string? HtmlBody { get; init; }

    /// <summary>
    /// Gets the file attachments (file paths)
    /// </summary>
    public IReadOnlyList<string> Attachments { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets additional headers
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the priority (normal, high, low)
    /// </summary>
    public MailPriority Priority { get; init; } = MailPriority.Normal;

    /// <summary>
    /// Gets when the message was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Email message priority levels
/// </summary>
public enum MailPriority
{
    /// <summary>
    /// Low priority
    /// </summary>
    Low,

    /// <summary>
    /// Normal priority (default)
    /// </summary>
    Normal,

    /// <summary>
    /// High priority
    /// </summary>
    High
}
