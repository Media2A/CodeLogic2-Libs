namespace CL.Mail.Models;

/// <summary>
/// Represents an email template
/// </summary>
public record MailTemplate
{
    /// <summary>
    /// Gets the unique template identifier
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the template name
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the template description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the email subject template (may contain variables)
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Gets the plain text body template
    /// </summary>
    public string? TextBody { get; init; }

    /// <summary>
    /// Gets the HTML body template
    /// </summary>
    public string? HtmlBody { get; init; }

    /// <summary>
    /// Gets the list of variable placeholders used in the template
    /// </summary>
    public IReadOnlyList<string> Variables { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets when the template was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets when the template was last modified
    /// </summary>
    public DateTime? ModifiedAt { get; init; }

    /// <summary>
    /// Gets any template metadata
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Creates a new mail template with the given parameters
    /// </summary>
    public static MailTemplate Create(
        string id,
        string subject,
        string? textBody = null,
        string? htmlBody = null,
        string? name = null,
        IReadOnlyList<string>? variables = null) =>
        new()
        {
            Id = id,
            Name = name ?? id,
            Subject = subject,
            TextBody = textBody,
            HtmlBody = htmlBody,
            Variables = variables ?? Array.Empty<string>()
        };
}

/// <summary>
/// Result of rendering a mail template
/// </summary>
public record RenderedTemplate
{
    /// <summary>
    /// Gets the rendered subject
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Gets the rendered plain text body
    /// </summary>
    public string? TextBody { get; init; }

    /// <summary>
    /// Gets the rendered HTML body
    /// </summary>
    public string? HtmlBody { get; init; }
}
