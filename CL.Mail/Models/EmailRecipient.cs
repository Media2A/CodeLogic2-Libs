namespace CL.Mail.Models;

/// <summary>
/// Represents an email recipient with optional display name
/// </summary>
public record EmailRecipient
{
    /// <summary>
    /// Gets the email address
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets the optional display name
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Creates a new recipient with just an email address
    /// </summary>
    public static EmailRecipient Create(string email) => new() { Email = email };

    /// <summary>
    /// Creates a new recipient with email and display name
    /// </summary>
    public static EmailRecipient Create(string email, string displayName) =>
        new() { Email = email, DisplayName = displayName };

    /// <summary>
    /// Returns a string representation of the recipient
    /// </summary>
    public override string ToString() =>
        string.IsNullOrWhiteSpace(DisplayName)
            ? Email
            : $"{DisplayName} <{Email}>";
}
