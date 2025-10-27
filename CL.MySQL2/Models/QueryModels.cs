namespace CL.MySQL2.Models;

/// <summary>
/// Represents the result of a database operation.
/// </summary>
/// <typeparam name="T">The type of data returned by the operation.</typeparam>
public class OperationResult<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the data returned by the operation.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception that occurred during the operation.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the number of rows affected by the operation.
    /// </summary>
    public int RowsAffected { get; set; }

    /// <summary>
    /// Creates a successful operation result.
    /// </summary>
    public static OperationResult<T> Ok(T? data, int rowsAffected = 0)
    {
        return new OperationResult<T>
        {
            Success = true,
            Data = data,
            RowsAffected = rowsAffected
        };
    }

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    public static OperationResult<T> Fail(string errorMessage, Exception? exception = null)
    {
        return new OperationResult<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}

/// <summary>
/// Represents a paginated result set.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Gets or sets the items in the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public long TotalItems { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Represents a condition for filtering query results.
/// </summary>
public class WhereCondition
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public required string Column { get; set; }

    /// <summary>
    /// Gets or sets the comparison operator (=, !=, >, <, >=, <=, LIKE, IN, etc.).
    /// </summary>
    public string Operator { get; set; } = "=";

    /// <summary>
    /// Gets or sets the value to compare against.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the logical operator to combine with the next condition (AND, OR).
    /// </summary>
    public string LogicalOperator { get; set; } = "AND";
}

/// <summary>
/// Represents an order by clause for sorting query results.
/// </summary>
public class OrderByClause
{
    /// <summary>
    /// Gets or sets the column name to sort by.
    /// </summary>
    public required string Column { get; set; }

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public SortOrder Order { get; set; } = SortOrder.Asc;
}

/// <summary>
/// Represents a join clause for combining tables.
/// </summary>
public class JoinClause
{
    /// <summary>
    /// Gets or sets the type of join (INNER, LEFT, RIGHT, FULL).
    /// </summary>
    public JoinType Type { get; set; } = JoinType.Inner;

    /// <summary>
    /// Gets or sets the table to join.
    /// </summary>
    public required string Table { get; set; }

    /// <summary>
    /// Gets or sets the join condition (e.g., "users.id = orders.user_id").
    /// </summary>
    public required string Condition { get; set; }
}

/// <summary>
/// Enumerates the types of SQL joins.
/// </summary>
public enum JoinType
{
    /// <summary>
    /// INNER JOIN - Returns rows that have matching values in both tables.
    /// </summary>
    Inner,

    /// <summary>
    /// LEFT JOIN - Returns all rows from the left table and matched rows from the right table.
    /// </summary>
    Left,

    /// <summary>
    /// RIGHT JOIN - Returns all rows from the right table and matched rows from the left table.
    /// </summary>
    Right,

    /// <summary>
    /// CROSS JOIN - Returns the Cartesian product of both tables.
    /// </summary>
    Cross
}

/// <summary>
/// Represents an aggregate function for query results.
/// </summary>
public class AggregateFunction
{
    /// <summary>
    /// Gets or sets the type of aggregate function.
    /// </summary>
    public AggregateType Type { get; set; }

    /// <summary>
    /// Gets or sets the column to apply the aggregate function to.
    /// </summary>
    public required string Column { get; set; }

    /// <summary>
    /// Gets or sets the alias for the aggregate result.
    /// </summary>
    public required string Alias { get; set; }
}

/// <summary>
/// Enumerates the types of aggregate functions.
/// </summary>
public enum AggregateType
{
    /// <summary>
    /// COUNT - Counts the number of rows.
    /// </summary>
    Count,

    /// <summary>
    /// SUM - Sums the values in a column.
    /// </summary>
    Sum,

    /// <summary>
    /// AVG - Calculates the average of values in a column.
    /// </summary>
    Avg,

    /// <summary>
    /// MIN - Finds the minimum value in a column.
    /// </summary>
    Min,

    /// <summary>
    /// MAX - Finds the maximum value in a column.
    /// </summary>
    Max
}

/// <summary>
/// Represents the result of a table synchronization operation.
/// </summary>
public class SyncResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the synchronization was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the name of the table that was synchronized.
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// Gets or sets the list of operations performed during synchronization.
    /// </summary>
    public List<string> Operations { get; set; } = new();

    /// <summary>
    /// Gets or sets error messages if synchronization failed.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the time taken to complete the synchronization.
    /// </summary>
    public TimeSpan Duration { get; set; }
}
