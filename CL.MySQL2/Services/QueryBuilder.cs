using CodeLogic.Abstractions;
using CL.MySQL2.Core;
using CL.MySQL2.Models;
using MySqlConnector;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CL.MySQL2.Services;

/// <summary>
/// Type-safe fluent query builder for constructing complex SQL queries using LINQ expressions.
/// Provides compile-time error checking and full IntelliSense support for database queries.
/// </summary>
public class QueryBuilder<T> where T : class, new()
{
    private readonly string _tableName;
    private readonly List<string> _selectColumns = new();
    private readonly List<WhereCondition> _whereConditions = new();
    private readonly List<OrderByClause> _orderByClauses = new();
    private readonly List<JoinClause> _joinClauses = new();
    private readonly List<AggregateFunction> _aggregateFunctions = new();
    private readonly List<string> _groupByColumns = new();
    private int? _limit;
    private int? _offset;
    private string _connectionId = "Default";
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger? _logger;

    public QueryBuilder(ConnectionManager connectionManager, ILogger? logger = null, string connectionId = "Default")
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger;
        _connectionId = connectionId;

        var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>();
        _tableName = tableAttr?.Name ?? typeof(T).Name;
    }

    /// <summary>
    /// Adds a LINQ-based WHERE condition (type-safe!).
    /// Example: .Where(u => u.IsActive == true)
    /// </summary>
    public QueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        var conditions = CL.MySQL2.Core.ExpressionVisitor.Parse(predicate);
        _whereConditions.AddRange(conditions);
        return this;
    }

    /// <summary>
    /// Adds a LINQ-based ORDER BY clause (ascending).
    /// Example: .OrderBy(u => u.CreatedAt)
    /// </summary>
    public QueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var (column, _) = CL.MySQL2.Core.ExpressionVisitor.ParseOrderBy(keySelector, descending: false);
        _orderByClauses.Add(new OrderByClause { Column = column, Order = SortOrder.Asc });
        return this;
    }

    /// <summary>
    /// Adds a LINQ-based ORDER BY clause (descending).
    /// Example: .OrderByDescending(u => u.CreatedAt)
    /// </summary>
    public QueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var (column, _) = CL.MySQL2.Core.ExpressionVisitor.ParseOrderBy(keySelector, descending: true);
        _orderByClauses.Add(new OrderByClause { Column = column, Order = SortOrder.Desc });
        return this;
    }

    /// <summary>
    /// Adds a LINQ-based secondary ORDER BY clause (ascending).
    /// Example: .OrderBy(u => u.IsActive).ThenBy(u => u.Email)
    /// </summary>
    public QueryBuilder<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var (column, _) = CL.MySQL2.Core.ExpressionVisitor.ParseOrderBy(keySelector, descending: false);
        _orderByClauses.Add(new OrderByClause { Column = column, Order = SortOrder.Asc });
        return this;
    }

    /// <summary>
    /// Adds a LINQ-based secondary ORDER BY clause (descending).
    /// Example: .OrderBy(u => u.IsActive).ThenByDescending(u => u.Email)
    /// </summary>
    public QueryBuilder<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var (column, _) = CL.MySQL2.Core.ExpressionVisitor.ParseOrderBy(keySelector, descending: true);
        _orderByClauses.Add(new OrderByClause { Column = column, Order = SortOrder.Desc });
        return this;
    }

    /// <summary>
    /// Adds a LINQ-based GROUP BY clause.
    /// Example: .GroupBy(u => u.Department)
    /// </summary>
    public QueryBuilder<T> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var columns = CL.MySQL2.Core.ExpressionVisitor.ParseGroupBy<T, TKey>(keySelector);
        _groupByColumns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Adds an INNER JOIN clause.
    /// Example: .InnerJoin("blog_categories", "blog_posts.category_id = blog_categories.id")
    /// </summary>
    public QueryBuilder<T> InnerJoin(string table, string condition)
    {
        _joinClauses.Add(new JoinClause
        {
            Type = JoinType.Inner,
            Table = table,
            Condition = condition
        });
        return this;
    }

    /// <summary>
    /// Adds a LEFT JOIN clause.
    /// Example: .LeftJoin("blog_categories", "blog_posts.category_id = blog_categories.id")
    /// </summary>
    public QueryBuilder<T> LeftJoin(string table, string condition)
    {
        _joinClauses.Add(new JoinClause
        {
            Type = JoinType.Left,
            Table = table,
            Condition = condition
        });
        return this;
    }

    /// <summary>
    /// Adds a RIGHT JOIN clause.
    /// Example: .RightJoin("blog_categories", "blog_posts.category_id = blog_categories.id")
    /// </summary>
    public QueryBuilder<T> RightJoin(string table, string condition)
    {
        _joinClauses.Add(new JoinClause
        {
            Type = JoinType.Right,
            Table = table,
            Condition = condition
        });
        return this;
    }

    /// <summary>
    /// Adds a CROSS JOIN clause.
    /// Example: .CrossJoin("blog_tags")
    /// </summary>
    public QueryBuilder<T> CrossJoin(string table)
    {
        _joinClauses.Add(new JoinClause
        {
            Type = JoinType.Cross,
            Table = table,
            Condition = "" // CROSS JOIN doesn't need a condition
        });
        return this;
    }

    /// <summary>
    /// Adds a LINQ-based SELECT clause (column projection).
    /// Example: .Select(u => new { u.Id, u.Email })
    /// </summary>
    public QueryBuilder<T> Select<TResult>(Expression<Func<T, TResult>> columns) where TResult : class
    {
        var selectedColumns = CL.MySQL2.Core.ExpressionVisitor.ParseSelect(
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
        var (columnName, _) = CL.MySQL2.Core.ExpressionVisitor.ParseOrderBy(column, false);
        return Aggregate(AggregateType.Sum, columnName, alias);
    }

    /// <summary>
    /// Adds a LINQ-based AVG aggregate.
    /// Example: .Avg(u => u.Rating, "average")
    /// </summary>
    public QueryBuilder<T> Avg<TKey>(Expression<Func<T, TKey>> column, string alias = "avg")
    {
        var (columnName, _) = CL.MySQL2.Core.ExpressionVisitor.ParseOrderBy(column, false);
        return Aggregate(AggregateType.Avg, columnName, alias);
    }

    /// <summary>
    /// Adds a LINQ-based MIN aggregate.
    /// Example: .Min(u => u.Price, "minimum")
    /// </summary>
    public QueryBuilder<T> Min<TKey>(Expression<Func<T, TKey>> column, string alias = "min")
    {
        var (columnName, _) = CL.MySQL2.Core.ExpressionVisitor.ParseOrderBy(column, false);
        return Aggregate(AggregateType.Min, columnName, alias);
    }

    /// <summary>
    /// Adds a LINQ-based MAX aggregate.
    /// Example: .Max(u => u.Price, "maximum")
    /// </summary>
    public QueryBuilder<T> Max<TKey>(Expression<Func<T, TKey>> column, string alias = "max")
    {
        var (columnName, _) = CL.MySQL2.Core.ExpressionVisitor.ParseOrderBy(column, false);
        return Aggregate(AggregateType.Max, columnName, alias);
    }

    /// <summary>
    /// Limits the number of results returned.
    /// </summary>
    public QueryBuilder<T> Limit(int limit)
    {
        _limit = limit;
        return this;
    }

    /// <summary>
    /// Sets the offset for pagination.
    /// </summary>
    public QueryBuilder<T> Offset(int offset)
    {
        _offset = offset;
        return this;
    }

    /// <summary>
    /// Sets the offset for pagination (alias for Offset).
    /// </summary>
    public QueryBuilder<T> Skip(int skip)
    {
        return Offset(skip);
    }

    /// <summary>
    /// Sets the limit for pagination (alias for Limit).
    /// </summary>
    public QueryBuilder<T> Take(int take)
    {
        return Limit(take);
    }

    /// <summary>
    /// Sets the connection ID to use for this query.
    /// </summary>
    public QueryBuilder<T> UseConnection(string connectionId)
    {
        _connectionId = connectionId;
        return this;
    }

    /// <summary>
    /// Executes the query and returns the results.
    /// </summary>
    public async Task<OperationResult<List<T>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = BuildSelectQuery();
            var startTime = DateTime.UtcNow;

            var config = _connectionManager.GetConfiguration(_connectionId);

            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                using var cmd = new MySqlCommand(sql, connection);
                AddParametersToCommand(cmd);

                if (config.EnableLogging)
                {
                    _logger?.Debug($"Executing query: {sql}");
                }

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var entities = new List<T>();

                while (await reader.ReadAsync(cancellationToken))
                {
                    entities.Add(MapReaderToEntity(reader));
                }

                var duration = DateTime.UtcNow - startTime;
                if (config.LogSlowQueries && duration.TotalMilliseconds > config.SlowQueryThreshold)
                {
                    _logger?.Warning($"Slow query detected ({duration.TotalMilliseconds}ms): {sql}");
                }

                return OperationResult<List<T>>.Ok(entities);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Query execution failed: {ex.Message}", ex);
            return OperationResult<List<T>>.Fail($"Query execution failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executes the query and returns a single result.
    /// </summary>
    public async Task<OperationResult<T>> ExecuteSingleAsync(CancellationToken cancellationToken = default)
    {
        Limit(1);
        var result = await ExecuteAsync(cancellationToken);

        if (!result.Success)
            return OperationResult<T>.Fail(result.ErrorMessage, result.Exception);

        return OperationResult<T>.Ok(result.Data?.FirstOrDefault());
    }

    /// <summary>
    /// Executes the query and returns the first result or null.
    /// </summary>
    public async Task<OperationResult<T>> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteSingleAsync(cancellationToken);
    }

    /// <summary>
    /// Executes the query and returns paginated results.
    /// </summary>
    public async Task<OperationResult<PagedResult<T>>> ExecutePagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get total count
            var countQuery = BuildCountQuery();

            var totalItems = await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                using var cmd = new MySqlCommand(countQuery, connection);
                AddParametersToCommand(cmd);

                return Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
            }, _connectionId, cancellationToken);

            // Get page data
            _offset = (pageNumber - 1) * pageSize;
            _limit = pageSize;

            var itemsResult = await ExecuteAsync(cancellationToken);

            if (!itemsResult.Success)
                return OperationResult<PagedResult<T>>.Fail(itemsResult.ErrorMessage, itemsResult.Exception);

            var pagedResult = new PagedResult<T>
            {
                Items = itemsResult.Data ?? new List<T>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return OperationResult<PagedResult<T>>.Ok(pagedResult);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Paged query execution failed: {ex.Message}", ex);
            return OperationResult<PagedResult<T>>.Fail($"Paged query execution failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executes a COUNT query and returns the count.
    /// </summary>
    public async Task<OperationResult<long>> CountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var countQuery = BuildCountQuery();

            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                using var cmd = new MySqlCommand(countQuery, connection);
                AddParametersToCommand(cmd);

                var count = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
                return OperationResult<long>.Ok(count);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Count query failed: {ex.Message}", ex);
            return OperationResult<long>.Fail($"Count query failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executes a DELETE query with WHERE conditions and returns the number of rows affected.
    /// Example: queryBuilder.Where(c => c.PostId == 123).DeleteAsync()
    /// </summary>
    public async Task<OperationResult<int>> DeleteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteQuery = BuildDeleteQuery();

            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                using var cmd = new MySqlCommand(deleteQuery, connection);
                AddParametersToCommand(cmd);

                var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return OperationResult<int>.Ok(rowsAffected);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Delete query failed: {ex.Message}", ex);
            return OperationResult<int>.Fail($"Delete query failed: {ex.Message}", ex);
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
                sb.Append($"{string.Join(", ", _selectColumns.Select(c => $"`{c}`"))}, {string.Join(", ", aggParts)}");
            }
            else
            {
                sb.Append(string.Join(", ", aggParts));
            }
        }
        else if (_selectColumns.Any())
        {
            sb.Append(string.Join(", ", _selectColumns.Select(c => $"`{c}`")));
        }
        else
        {
            sb.Append("*");
        }

        // FROM clause
        sb.Append($" FROM `{_tableName}`");

        // JOIN clauses
        foreach (var join in _joinClauses)
        {
            var joinType = join.Type switch
            {
                JoinType.Left => "LEFT JOIN",
                JoinType.Right => "RIGHT JOIN",
                JoinType.Cross => "CROSS JOIN",
                _ => "INNER JOIN"
            };
            sb.Append($" {joinType} `{join.Table}` ON {join.Condition}");
        }

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
                    sb.Append($"`{condition.Column}` IN ({string.Join(", ", paramNames)})");
                }
                else if (condition.Operator.Equals("BETWEEN", StringComparison.OrdinalIgnoreCase) &&
                         condition.Value is Array betweenArr && betweenArr.Length == 2)
                {
                    sb.Append($"`{condition.Column}` BETWEEN @p{i}_0 AND @p{i}_1");
                }
                else
                {
                    sb.Append($"`{condition.Column}` {condition.Operator} @p{i}");
                }
            }
        }

        // GROUP BY clause
        if (_groupByColumns.Any())
        {
            sb.Append($" GROUP BY {string.Join(", ", _groupByColumns.Select(c => $"`{c}`"))}");
        }

        // ORDER BY clause
        if (_orderByClauses.Any())
        {
            sb.Append(" ORDER BY ");
            sb.Append(string.Join(", ", _orderByClauses.Select(o =>
                $"`{o.Column}` {(o.Order == SortOrder.Asc ? "ASC" : "DESC")}")));
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
        sb.Append("SELECT COUNT(*) FROM `");
        sb.Append(_tableName);
        sb.Append("`");

        // JOIN clauses
        foreach (var join in _joinClauses)
        {
            var joinType = join.Type switch
            {
                JoinType.Left => "LEFT JOIN",
                JoinType.Right => "RIGHT JOIN",
                JoinType.Cross => "CROSS JOIN",
                _ => "INNER JOIN"
            };
            sb.Append($" {joinType} `{join.Table}` ON {join.Condition}");
        }

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
                    sb.Append($"`{condition.Column}` IN ({string.Join(", ", paramNames)})");
                }
                else if (condition.Operator.Equals("BETWEEN", StringComparison.OrdinalIgnoreCase) &&
                         condition.Value is Array betweenArr && betweenArr.Length == 2)
                {
                    sb.Append($"`{condition.Column}` BETWEEN @p{i}_0 AND @p{i}_1");
                }
                else
                {
                    sb.Append($"`{condition.Column}` {condition.Operator} @p{i}");
                }
            }
        }

        return sb.ToString();
    }

    private string BuildDeleteQuery()
    {
        var sb = new StringBuilder();
        sb.Append("DELETE FROM `");
        sb.Append(_tableName);
        sb.Append("`");

        // WHERE clause (same logic as BuildCountQuery)
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
                    sb.Append($"`{condition.Column}` IN ({string.Join(", ", paramNames)})");
                }
                else if (condition.Operator.Equals("BETWEEN", StringComparison.OrdinalIgnoreCase) &&
                         condition.Value is Array betweenArr && betweenArr.Length == 2)
                {
                    sb.Append($"`{condition.Column}` BETWEEN @p{i}_0 AND @p{i}_1");
                }
                else
                {
                    sb.Append($"`{condition.Column}` {condition.Operator} @p{i}");
                }
            }
        }

        return sb.ToString();
    }

    private void AddParametersToCommand(MySqlCommand cmd)
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
            else if (condition.Operator.Equals("BETWEEN", StringComparison.OrdinalIgnoreCase) &&
                     condition.Value is Array betweenArr && betweenArr.Length == 2)
            {
                cmd.Parameters.AddWithValue($"@p{i}_0", betweenArr.GetValue(0) ?? DBNull.Value);
                cmd.Parameters.AddWithValue($"@p{i}_1", betweenArr.GetValue(1) ?? DBNull.Value);
            }
            else
            {
                cmd.Parameters.AddWithValue($"@p{i}", condition.Value ?? DBNull.Value);
            }
        }
    }

    private T MapReaderToEntity(MySqlDataReader reader)
    {
        var entity = new T();
        var properties = typeof(T).GetProperties()
            .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null)
            .ToArray();

        foreach (var prop in properties)
        {
            var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr == null)
                continue;

            var columnName = columnAttr.Name ?? prop.Name;

            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                var value = reader.GetValue(ordinal);

                if (value != DBNull.Value)
                {
                    var convertedValue = TypeConverter.FromMySql(value, columnAttr.DataType, prop.PropertyType);
                    prop.SetValue(entity, convertedValue);
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

/// <summary>
/// Non-generic query builder factory for creating query builders for specific types.
/// </summary>
public class QueryBuilder
{
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger? _logger;
    private readonly string _connectionId;

    public QueryBuilder(ConnectionManager connectionManager, ILogger? logger = null, string connectionId = "Default")
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger;
        _connectionId = connectionId;
    }

    /// <summary>
    /// Creates a query builder for the specified model type.
    /// </summary>
    public QueryBuilder<T> For<T>() where T : class, new()
    {
        return new QueryBuilder<T>(_connectionManager, _logger, _connectionId);
    }
}
