namespace CL.SQLite.Models;

/// <summary>
/// Represents a SQL query with parameters
/// </summary>
public class SQLiteQuery
{
    /// <summary>
    /// Gets or sets the SQL query string
    /// </summary>
    public required string QueryString { get; init; }

    /// <summary>
    /// Gets or sets the query parameters
    /// </summary>
    public Dictionary<string, object?> Parameters { get; init; } = new();
}

/// <summary>
/// Represents the result of a table synchronization operation
/// </summary>
public record TableSyncResult
{
    /// <summary>
    /// Gets whether the synchronization was successful
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the message describing the result
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets any exception that occurred
    /// </summary>
    public Exception? Exception { get; init; }

    public static TableSyncResult Succeeded(string message) =>
        new() { Success = true, Message = message };

    public static TableSyncResult Failed(string message, Exception? exception = null) =>
        new() { Success = false, Message = message, Exception = exception };
}

/// <summary>
/// Transaction isolation levels for SQLite
/// </summary>
public enum TransactionIsolation
{
    /// <summary>
    /// Deferred transaction - locks are acquired when needed
    /// </summary>
    Deferred,

    /// <summary>
    /// Immediate transaction - acquires a reserved lock immediately
    /// </summary>
    Immediate,

    /// <summary>
    /// Exclusive transaction - acquires an exclusive lock immediately
    /// </summary>
    Exclusive
}
