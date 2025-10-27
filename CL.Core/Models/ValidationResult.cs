namespace CL.Core.Models;

/// <summary>
/// Result of a validation operation
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Whether validation passed
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();

    public static ValidationResult Valid() => new() { IsValid = true };

    public static ValidationResult Invalid(params ValidationError[] errors) =>
        new() { IsValid = false, Errors = errors };

    public static ValidationResult Invalid(IEnumerable<ValidationError> errors) =>
        new() { IsValid = false, Errors = errors.ToList() };
}

/// <summary>
/// Represents a single validation error
/// </summary>
public record ValidationError
{
    /// <summary>
    /// Property or field that failed validation
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Error message
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string? Code { get; init; }
}
