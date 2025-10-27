namespace CL.SystemStats.Models;

/// <summary>
/// Result of a system stats operation
/// </summary>
public record SystemStatsResult
{
    /// <summary>
    /// Gets whether the operation was successful
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error code if the operation failed
    /// </summary>
    public SystemStatsError Error { get; init; } = SystemStatsError.None;

    /// <summary>
    /// Gets the error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets when the operation was performed
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static SystemStatsResult Success() =>
        new() { IsSuccess = true };

    /// <summary>
    /// Creates a failure result
    /// </summary>
    public static SystemStatsResult Failure(SystemStatsError error, string? message = null) =>
        new() { IsSuccess = false, Error = error, ErrorMessage = message };
}

/// <summary>
/// Result of a system stats operation with a value
/// </summary>
public record SystemStatsResult<T>
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
    public SystemStatsError Error { get; init; } = SystemStatsError.None;

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
    public static SystemStatsResult<T> Success(T value) =>
        new() { IsSuccess = true, Value = value };

    /// <summary>
    /// Creates a failure result
    /// </summary>
    public static SystemStatsResult<T> Failure(SystemStatsError error, string? message = null) =>
        new() { IsSuccess = false, Error = error, ErrorMessage = message };
}
