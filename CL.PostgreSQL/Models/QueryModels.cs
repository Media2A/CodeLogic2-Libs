namespace CL.PostgreSQL.Models;

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
/// Represents a WHERE condition for database queries.
/// </summary>
public class WhereCondition
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public required string Column { get; set; }

    /// <summary>
    /// Gets or sets the comparison operator (=, !=, <, >, etc.).
    /// </summary>
    public string Operator { get; set; } = "=";

    /// <summary>
    /// Gets or sets the value to compare against.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the logical operator for combining with other conditions (AND, OR).
    /// </summary>
    public string LogicalOperator { get; set; } = "AND";
}
