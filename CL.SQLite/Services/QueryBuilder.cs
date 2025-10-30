using CodeLogic.Abstractions;
using CodeLogic.Models;
using CL.SQLite.Models;
using Microsoft.Data.Sqlite;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CL.SQLite.Services;

/// <summary>
/// Type-safe fluent query builder for constructing complex SQL queries using LINQ expressions.
/// Provides compile-time error checking and full IntelliSense support for SQLite database queries.
/// </summary>
public class QueryBuilder<T> where T : class, new()
{
    private readonly string _tableName;
    private readonly List<string> _selectColumns = new();
    private readonly List<WhereCondition> _whereConditions = new();
    private readonly List<OrderByClause> _orderByClauses = new();
    private readonly List<AggregateFunction> _aggregateFunctions = new();
    private readonly List<string> _groupByColumns = new();
    private int? _limit;
    private int? _offset;
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger _logger;

    public QueryBuilder(ConnectionManager connectionManager, ILogger logger)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var tableAttr = typeof(T).GetCustomAttribute<SQLiteTableAttribute>();
        _tableName = tableAttr?.TableName ?? typeof(T).Name;
    }

    /// <summary>
    /// Adds a LINQ-based WHERE condition (type-safe!).
    /// Example: .Where(u => u.IsActive == true)
    /// </summary>
    public QueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        var conditions = ExpressionVisitor.Parse(predicate);
        _whereConditions.AddRange(conditions);
        return this;
    }

    /// <summary>
    /// Adds a LINQ-based ORDER BY clause (ascending).
    /// Example: .OrderBy(u => u.CreatedAt)
    /// </summary>
    public QueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var (column, _) = ExpressionVisitor.ParseOrderBy(keySelector, descending: false);
        _orderByClauses.Add(new OrderByClause { Column = column, Order = SortOrder.Asc });
        return this;
    }

    /// <summary>
    /// Adds a LINQ-based ORDER BY clause (descending).
    /// Example: .OrderByDescending(u => u.CreatedAt)
    /// </summary>
    public QueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var (column, _) = ExpressionVisitor.ParseOrderBy(keySelector, descending: true);
        _orderByClauses.Add(new OrderByClause { Column = column, Order = SortOrder.Desc });
        return this;
    }

    /// <summary>
    /// Adds a LINQ-based secondary ORDER BY clause (ascending).
    /// Example: .OrderBy(u => u.IsActive).ThenBy(u => u.Email)
    /// </summary>
    public QueryBuilder<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var (column, _) = ExpressionVisitor.ParseOrderBy(keySelector, descending: false);
        _orderByClauses.Add(new OrderByClause { Column = column, Order = SortOrder.Asc });
        return this;
    }

    /// <summary>
    /// Adds a LINQ-based secondary ORDER BY clause (descending).
    /// Example: .OrderBy(u => u.IsActive).ThenByDescending(u => u.Email)
    /// </summary>
    public QueryBuilder<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var (column, _) = ExpressionVisitor.ParseOrderBy(keySelector, descending: true);
        _orderByClauses.Add(new OrderByClause { Column = column, Order = SortOrder.Desc });
        return this;
    }

    /// <summary>
    /// Adds a LINQ-based GROUP BY clause.
    /// Example: .GroupBy(u => u.Department)
    /// </summary>
    public QueryBuilder<T> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var columns = ExpressionVisitor.ParseGroupBy<T, TKey>(keySelector);
        _groupByColumns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Adds a LINQ-based SELECT clause (column projection).
    /// Example: .Select(u => new { u.Id, u.Email })
    /// </summary>
    public QueryBuilder<T> Select<TResult>(Expression<Func<T, TResult>> columns) where TResult : class
    {
        var selectedColumns = ExpressionVisitor.ParseSelect(
            Expression.Lambda<Func<T, object?>>(
                Expression.Convert(columns.Body, typeof(object)),
                columns.Parameters
            )
        );
        _selectColumns.AddRange(selectedColumns);
        return this;
    }

    // Internal aggregate helper
    private QueryBuilder<T> Aggregate(AggregateType type, string column, string alias)
    {
        _aggregateFunctions.Add(new AggregateFunction
        {
            Type = type,
            Column = column,
            Alias = alias
        });
        return this;
    }

    /// <summary>
    /// Adds a LINQ-based SUM aggregate.
    /// Example: .Sum(u => u.TotalAmount, "total")
    /// </summary>
    public QueryBuilder<T> Sum<TKey>(Expression<Func<T, TKey>> column, string alias = "sum")
    {
        var (columnName, _) = ExpressionVisitor.ParseOrderBy(column, false);
        return Aggregate(AggregateType.Sum, columnName, alias);
    }

    /// <summary>
    /// Adds a LINQ-based AVG aggregate.
    /// Example: .Avg(u => u.Rating, "average")
    /// </summary>
    public QueryBuilder<T> Avg<TKey>(Expression<Func<T, TKey>> column, string alias = "avg")
    {
        var (columnName, _) = ExpressionVisitor.ParseOrderBy(column, false);
        return Aggregate(AggregateType.Avg, columnName, alias);
    }

    /// <summary>
    /// Adds a LINQ-based MIN aggregate.
    /// Example: .Min(u => u.Price, "minimum")
    /// </summary>
    public QueryBuilder<T> Min<TKey>(Expression<Func<T, TKey>> column, string alias = "min")
    {
        var (columnName, _) = ExpressionVisitor.ParseOrderBy(column, false);
        return Aggregate(AggregateType.Min, columnName, alias);
    }

    /// <summary>
    /// Adds a LINQ-based MAX aggregate.
    /// Example: .Max(u => u.Price, "maximum")
    /// </summary>
    public QueryBuilder<T> Max<TKey>(Expression<Func<T, TKey>> column, string alias = "max")
    {
        var (columnName, _) = ExpressionVisitor.ParseOrderBy(column, false);
        return Aggregate(AggregateType.Max, columnName, alias);
    }

    /// <summary>
    /// Limits the number of results returned.
    /// </summary>
    public QueryBuilder<T> Take(int count)
    {
        _limit = count;
        return this;
    }

    /// <summary>
    /// Sets the offset for pagination.
    /// </summary>
    public QueryBuilder<T> Skip(int skip)
    {
        _offset = skip;
        return this;
    }

    /// <summary>
    /// Executes the query and returns the results.
    /// </summary>
    public async Task<Result<List<T>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = BuildSelectQuery();
            var startTime = DateTime.UtcNow;

            return await _connectionManager.ExecuteAsync(async connection =>
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                AddParametersToCommand(cmd);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var entities = new List<T>();

                while (await reader.ReadAsync(cancellationToken))
                {
                    entities.Add(MapReaderToEntity(reader));
                }

                var duration = DateTime.UtcNow - startTime;
                _logger.Debug($"Query executed in {duration.TotalMilliseconds}ms: {sql}");

                return Result<List<T>>.Success(entities);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Query execution failed: {ex.Message}", ex);
            return Result<List<T>>.Failure($"Query execution failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executes the query and returns the first result or null.
    /// </summary>
    public async Task<Result<T?>> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        _limit = 1;
        var result = await ExecuteAsync(cancellationToken);

        if (!result.IsSuccess)
            return Result<T?>.Failure(result.ErrorMessage ?? "Failed to execute query", result.Exception);

        return Result<T?>.Success(result.Value?.FirstOrDefault());
    }

    /// <summary>
    /// Executes the query and returns paginated results.
    /// </summary>
    public async Task<Result<PagedResult<T>>> ToPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get total count
            var countQuery = BuildCountQuery();

            var totalItems = await _connectionManager.ExecuteAsync(async connection =>
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = countQuery;
                AddParametersToCommand(cmd);

                return Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
            }, cancellationToken);

            // Get page data
            _offset = (pageNumber - 1) * pageSize;
            _limit = pageSize;

            var itemsResult = await ExecuteAsync(cancellationToken);

            if (!itemsResult.IsSuccess)
                return Result<PagedResult<T>>.Failure(itemsResult.ErrorMessage ?? "Failed to execute query", itemsResult.Exception);

            var pagedResult = new PagedResult<T>
            {
                Items = itemsResult.Value ?? new List<T>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return Result<PagedResult<T>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.Error($"Paged query execution failed: {ex.Message}", ex);
            return Result<PagedResult<T>>.Failure($"Paged query execution failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executes a COUNT query and returns the count.
    /// </summary>
    public async Task<Result<long>> CountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var countQuery = BuildCountQuery();

            return await _connectionManager.ExecuteAsync(async connection =>
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = countQuery;
                AddParametersToCommand(cmd);

                var count = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
                return Result<long>.Success(count);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Count query failed: {ex.Message}", ex);
            return Result<long>.Failure($"Count query failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Returns the SQL query that would be executed (for debugging).
    /// </summary>
    public string ToSql()
    {
        return BuildSelectQuery();
    }

    private string BuildSelectQuery()
    {
        var sb = new StringBuilder();

        // SELECT clause
        sb.Append("SELECT ");
        if (_aggregateFunctions.Any())
        {
            var aggParts = _aggregateFunctions.Select(a =>
                $"{a.Type.ToString().ToUpper()}({a.Column}) AS {a.Alias}");

            if (_selectColumns.Any())
            {
                sb.Append($"{string.Join(", ", _selectColumns)}, {string.Join(", ", aggParts)}");
            }
            else
            {
                sb.Append(string.Join(", ", aggParts));
            }
        }
        else if (_selectColumns.Any())
        {
            sb.Append(string.Join(", ", _selectColumns));
        }
        else
        {
            sb.Append("*");
        }

        // FROM clause
        sb.Append($" FROM {_tableName}");

        // WHERE clause
        if (_whereConditions.Any())
        {
            sb.Append(" WHERE ");
            for (int i = 0; i < _whereConditions.Count; i++)
            {
                var condition = _whereConditions[i];
                if (i > 0)
                    sb.Append($" {condition.LogicalOperator} ");

                if (condition.Operator.Equals("IN", StringComparison.OrdinalIgnoreCase) &&
                    condition.Value is Array arr)
                {
                    var paramNames = new List<string>();
                    for (int j = 0; j < arr.Length; j++)
                    {
                        paramNames.Add($"@p{i}_{j}");
                    }
                    sb.Append($"{condition.Column} IN ({string.Join(", ", paramNames)})");
                }
                else
                {
                    sb.Append($"{condition.Column} {condition.Operator} @p{i}");
                }
            }
        }

        // GROUP BY clause
        if (_groupByColumns.Any())
        {
            sb.Append($" GROUP BY {string.Join(", ", _groupByColumns)}");
        }

        // ORDER BY clause
        if (_orderByClauses.Any())
        {
            sb.Append(" ORDER BY ");
            sb.Append(string.Join(", ", _orderByClauses.Select(o =>
                $"{o.Column} {(o.Order == SortOrder.Asc ? "ASC" : "DESC")}")));
        }

        // LIMIT and OFFSET
        if (_limit.HasValue)
        {
            sb.Append($" LIMIT {_limit.Value}");
        }

        if (_offset.HasValue)
        {
            sb.Append($" OFFSET {_offset.Value}");
        }

        return sb.ToString();
    }

    private string BuildCountQuery()
    {
        var sb = new StringBuilder();
        sb.Append($"SELECT COUNT(*) FROM {_tableName}");

        // WHERE clause
        if (_whereConditions.Any())
        {
            sb.Append(" WHERE ");
            for (int i = 0; i < _whereConditions.Count; i++)
            {
                var condition = _whereConditions[i];
                if (i > 0)
                    sb.Append($" {condition.LogicalOperator} ");

                if (condition.Operator.Equals("IN", StringComparison.OrdinalIgnoreCase) &&
                    condition.Value is Array arr)
                {
                    var paramNames = new List<string>();
                    for (int j = 0; j < arr.Length; j++)
                    {
                        paramNames.Add($"@p{i}_{j}");
                    }
                    sb.Append($"{condition.Column} IN ({string.Join(", ", paramNames)})");
                }
                else
                {
                    sb.Append($"{condition.Column} {condition.Operator} @p{i}");
                }
            }
        }

        return sb.ToString();
    }

    private void AddParametersToCommand(SqliteCommand cmd)
    {
        for (int i = 0; i < _whereConditions.Count; i++)
        {
            var condition = _whereConditions[i];

            if (condition.Operator.Equals("IN", StringComparison.OrdinalIgnoreCase) &&
                condition.Value is Array arr)
            {
                for (int j = 0; j < arr.Length; j++)
                {
                    cmd.Parameters.AddWithValue($"@p{i}_{j}", arr.GetValue(j) ?? DBNull.Value);
                }
            }
            else
            {
                cmd.Parameters.AddWithValue($"@p{i}", condition.Value ?? DBNull.Value);
            }
        }
    }

    private T MapReaderToEntity(SqliteDataReader reader)
    {
        var entity = new T();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var colAttr = prop.GetCustomAttribute<SQLiteColumnAttribute>();
            var columnName = colAttr?.ColumnName ?? prop.Name;

            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                var value = reader.GetValue(ordinal);

                if (value != DBNull.Value)
                {
                    prop.SetValue(entity, value);
                }
            }
            catch
            {
                // Column doesn't exist in result set, skip
            }
        }

        return entity;
    }
}
