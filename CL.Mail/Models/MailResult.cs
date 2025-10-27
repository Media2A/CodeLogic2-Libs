namespace CL.Mail.Models;

/// <summary>
/// Represents the result of a mail operation
/// </summary>
public record MailResult
{
    /// <summary>
    /// Gets whether the operation was successful
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error code if the operation failed
    /// </summary>
    public MailError Error { get; init; } = MailError.None;

    /// <summary>
    /// Gets the error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the message ID returned by the server (if successful)
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Gets when the operation was performed
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static MailResult Success(string? messageId = null) =>
        new() { IsSuccess = true, MessageId = messageId };

    /// <summary>
    /// Creates a failure result
    /// </summary>
    public static MailResult Failure(MailError error, string? message = null) =>
        new() { IsSuccess = false, Error = error, ErrorMessage = message };
}

/// <summary>
/// Represents the result of a mail operation with a value
/// </summary>
public record MailResult<T>
{
    /// <summary>
    /// Gets whether the operation was successful
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the result value if successful
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Gets the error code if the operation failed
    /// </summary>
    public MailError Error { get; init; } = MailError.None;

    /// <summary>
    /// Gets the error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets when the operation was performed
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    public static MailResult<T> Success(T value) =>
        new() { IsSuccess = true, Value = value };

    /// <summary>
    /// Creates a failure result
    /// </summary>
    public static MailResult<T> Failure(MailError error, string? message = null) =>
        new() { IsSuccess = false, Error = error, ErrorMessage = message };
}
