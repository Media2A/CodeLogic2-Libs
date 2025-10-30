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

/// <summary>
/// Represents a WHERE condition for LINQ query building
/// </summary>
public class WhereCondition
{
    /// <summary>
    /// Gets or sets the column name
    /// </summary>
    public required string Column { get; set; }

    /// <summary>
    /// Gets or sets the comparison operator (=, !=, >, <, >=, <=, LIKE, IN, IS, IS NOT)
    /// </summary>
    public required string Operator { get; set; }

    /// <summary>
    /// Gets or sets the value to compare against
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the logical operator (AND, OR)
    /// </summary>
    public string LogicalOperator { get; set; } = "AND";
}

/// <summary>
/// Represents an ORDER BY clause
/// </summary>
public class OrderByClause
{
    /// <summary>
    /// Gets or sets the column name
    /// </summary>
    public required string Column { get; set; }

    /// <summary>
    /// Gets or sets the sort order (Asc or Desc)
    /// </summary>
    public required SortOrder Order { get; set; }
}

/// <summary>
/// Sort order enumeration
/// </summary>
public enum SortOrder
{
    /// <summary>
    /// Ascending order
    /// </summary>
    Asc,

    /// <summary>
    /// Descending order
    /// </summary>
    Desc
}

/// <summary>
/// Represents an aggregate function (SUM, AVG, MIN, MAX, COUNT)
/// </summary>
public class AggregateFunction
{
    /// <summary>
    /// Gets or sets the aggregate type
    /// </summary>
    public required AggregateType Type { get; set; }

    /// <summary>
    /// Gets or sets the column name to aggregate
    /// </summary>
    public required string Column { get; set; }

    /// <summary>
    /// Gets or sets the alias for the result
    /// </summary>
    public required string Alias { get; set; }
}

/// <summary>
/// Aggregate function type enumeration
/// </summary>
public enum AggregateType
{
    /// <summary>
    /// SUM aggregate
    /// </summary>
    Sum,

    /// <summary>
    /// AVG aggregate
    /// </summary>
    Avg,

    /// <summary>
    /// MIN aggregate
    /// </summary>
    Min,

    /// <summary>
    /// MAX aggregate
    /// </summary>
    Max,

    /// <summary>
    /// COUNT aggregate
    /// </summary>
    Count
}

/// <summary>
/// Represents paginated results
/// </summary>
public class PagedResult<T> where T : class
{
    /// <summary>
    /// Gets or sets the items on this page
    /// </summary>
    public required List<T> Items { get; set; }

    /// <summary>
    /// Gets or sets the current page number
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of items
    /// </summary>
    public long TotalItems { get; set; }

    /// <summary>
    /// Gets the total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
