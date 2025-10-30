using CodeLogic.Abstractions;
using CL.PostgreSQL.Core;
using CL.PostgreSQL.Models;
using Npgsql;
using System.Reflection;
using System.Text;

namespace CL.PostgreSQL.Services;

/// <summary>
/// Fluent query builder for constructing complex SQL queries.
/// Provides a type-safe, intuitive API for building SELECT, INSERT, UPDATE, and DELETE queries.
/// </summary>
public class QueryBuilder<T> where T : class, new()
{
    private readonly string _tableName;
    private readonly string _schemaName;
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
        _schemaName = tableAttr?.Schema ?? "public";
    }

    /// <summary>
    /// Specifies which columns to select. If not called, SELECT * is used.
    /// </summary>
    public QueryBuilder<T> Select(params string[] columns)
    {
        _selectColumns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Adds a WHERE condition to the query.
    /// </summary>
    public QueryBuilder<T> Where(string column, string @operator, object? value, string logicalOperator = "AND")
    {
        _whereConditions.Add(new WhereCondition
        {
            Column = column,
            Operator = @operator,
            Value = value,
            LogicalOperator = logicalOperator
        });
        return this;
    }

    /// <summary>
    /// Adds a WHERE column = value condition.
    /// </summary>
    public QueryBuilder<T> WhereEquals(string column, object? value)
    {
        return Where(column, "=", value);
    }

    /// <summary>
    /// Adds a WHERE column IN (values) condition.
    /// </summary>
    public QueryBuilder<T> WhereIn(string column, params object[] values)
    {
        return Where(column, "IN", values);
    }

    /// <summary>
    /// Adds a WHERE column LIKE pattern condition.
    /// </summary>
    public QueryBuilder<T> WhereLike(string column, string pattern)
    {
        return Where(column, "LIKE", pattern);
    }

    /// <summary>
    /// Adds a WHERE column > value condition.
    /// </summary>
    public QueryBuilder<T> WhereGreaterThan(string column, object? value)
    {
        return Where(column, ">", value);
    }

    /// <summary>
    /// Adds a WHERE column < value condition.
    /// </summary>
    public QueryBuilder<T> WhereLessThan(string column, object? value)
    {
        return Where(column, "<", value);
    }

    /// <summary>
    /// Adds a WHERE column BETWEEN value1 AND value2 condition.
    /// </summary>
    public QueryBuilder<T> WhereBetween(string column, object? value1, object? value2)
    {
        return Where(column, "BETWEEN", new[] { value1, value2 });
    }

    /// <summary>
    /// Adds an ORDER BY clause.
    /// </summary>
    public QueryBuilder<T> OrderBy(string column, SortOrder order = SortOrder.Asc)
    {
        _orderByClauses.Add(new OrderByClause
        {
            Column = column,
            Order = order
        });
        return this;
    }

    /// <summary>
    /// Adds an ORDER BY column ASC clause.
    /// </summary>
    public QueryBuilder<T> OrderByAsc(string column)
    {
        return OrderBy(column, SortOrder.Asc);
    }

    /// <summary>
    /// Adds an ORDER BY column DESC clause.
    /// </summary>
    public QueryBuilder<T> OrderByDesc(string column)
    {
        return OrderBy(column, SortOrder.Desc);
    }

    /// <summary>
    /// Adds a JOIN clause.
    /// </summary>
    public QueryBuilder<T> Join(string table, string condition, JoinType joinType = JoinType.Inner)
    {
        _joinClauses.Add(new JoinClause
        {
            Type = joinType,
            Table = table,
            Condition = condition
        });
        return this;
    }

    /// <summary>
    /// Adds an INNER JOIN clause.
    /// </summary>
    public QueryBuilder<T> InnerJoin(string table, string condition)
    {
        return Join(table, condition, JoinType.Inner);
    }

    /// <summary>
    /// Adds a LEFT JOIN clause.
    /// </summary>
    public QueryBuilder<T> LeftJoin(string table, string condition)
    {
        return Join(table, condition, JoinType.Left);
    }

    /// <summary>
    /// Adds a RIGHT JOIN clause.
    /// </summary>
    public QueryBuilder<T> RightJoin(string table, string condition)
    {
        return Join(table, condition, JoinType.Right);
    }

    /// <summary>
    /// Adds a GROUP BY clause.
    /// </summary>
    public QueryBuilder<T> GroupBy(params string[] columns)
    {
        _groupByColumns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Adds an aggregate function.
    /// </summary>
    public QueryBuilder<T> Aggregate(AggregateType type, string column, string alias)
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
    /// Adds a COUNT aggregate.
    /// </summary>
    public QueryBuilder<T> Count(string column = "*", string alias = "count")
    {
        return Aggregate(AggregateType.Count, column, alias);
    }

    /// <summary>
    /// Adds a SUM aggregate.
    /// </summary>
    public QueryBuilder<T> Sum(string column, string alias = "sum")
    {
        return Aggregate(AggregateType.Sum, column, alias);
    }

    /// <summary>
    /// Adds an AVG aggregate.
    /// </summary>
    public QueryBuilder<T> Avg(string column, string alias = "avg")
    {
        return Aggregate(AggregateType.Avg, column, alias);
    }

    /// <summary>
    /// Adds a MIN aggregate.
    /// </summary>
    public QueryBuilder<T> Min(string column, string alias = "min")
    {
        return Aggregate(AggregateType.Min, column, alias);
    }

    /// <summary>
    /// Adds a MAX aggregate.
    /// </summary>
    public QueryBuilder<T> Max(string column, string alias = "max")
    {
        return Aggregate(AggregateType.Max, column, alias);
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
    /// Alias for Limit.
    /// </summary>
    public QueryBuilder<T> Take(int count) => Limit(count);

    /// <summary>
    /// Sets the offset for pagination.
    /// </summary>
    public QueryBuilder<T> Offset(int offset)
    {
        _offset = offset;
        return this;
    }

    /// <summary>
    /// Alias for Offset.
    /// </summary>
    public QueryBuilder<T> Skip(int count) => Offset(count);

    /// <summary>
    /// Executes the query and returns all results.
    /// </summary>
    public async Task<List<T>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = BuildSelectSql();
            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                using var cmd = new NpgsqlCommand(sql, connection);
                AddParametersToCommand(cmd);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var results = new List<T>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    var entity = MapReaderToEntity(reader);
                    results.Add(entity);
                }
                return results;
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to execute query: {ex.Message}", ex);
            return new List<T>();
        }
    }

    /// <summary>
    /// Executes the query and returns the first result.
    /// </summary>
    public async Task<T?> ExecuteSingleAsync(CancellationToken cancellationToken = default)
    {
        var results = await ExecuteAsync(cancellationToken);
        return results.FirstOrDefault();
    }

    /// <summary>
    /// Executes the query and returns the first result or default.
    /// </summary>
    public async Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteSingleAsync(cancellationToken);
    }

    /// <summary>
    /// Executes the query and returns paginated results.
    /// </summary>
    public async Task<PagedResult<T>> ExecutePagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new PagedResult<T>
            {
                PageNumber = page,
                PageSize = pageSize
            };

            // Get total count
            var countSql = BuildCountSql();
            var totalCount = await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                using var cmd = new NpgsqlCommand(countSql, connection);
                AddParametersToCommand(cmd);
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                return result != null ? Convert.ToInt64(result) : 0;
            }, _connectionId, cancellationToken);

            results.TotalItems = totalCount;

            // Get paged results
            var offset = (page - 1) * pageSize;
            var sql = BuildSelectSql() + $" LIMIT {pageSize} OFFSET {offset}";

            var items = await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                using var cmd = new NpgsqlCommand(sql, connection);
                AddParametersToCommand(cmd);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var list = new List<T>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    var entity = MapReaderToEntity(reader);
                    list.Add(entity);
                }
                return list;
            }, _connectionId, cancellationToken);

            results.Items = items;
            return results;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to execute paged query: {ex.Message}", ex);
            return new PagedResult<T> { PageNumber = page, PageSize = pageSize };
        }
    }

    /// <summary>
    /// Executes a count query.
    /// </summary>
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = BuildCountSql();
            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                using var cmd = new NpgsqlCommand(sql, connection);
                AddParametersToCommand(cmd);
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                return result != null ? Convert.ToInt64(result) : 0;
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to execute count query: {ex.Message}", ex);
            return 0;
        }
    }

    /// <summary>
    /// Builds and returns the SQL query string for debugging.
    /// </summary>
    public string ToSql()
    {
        return BuildSelectSql();
    }

    // Private Helper Methods

    private string BuildSelectSql()
    {
        var sb = new StringBuilder();

        // SELECT clause
        sb.Append("SELECT ");
        if (_aggregateFunctions.Count > 0)
        {
            var aggregates = _aggregateFunctions.Select(a =>
                $"{a.Type}(\"{a.Column}\") AS \"{a.Alias}\"");
            sb.Append(string.Join(", ", aggregates));
        }
        else if (_selectColumns.Count > 0)
        {
            sb.Append(string.Join(", ", _selectColumns.Select(c => $"\"{c}\"")));
        }
        else
        {
            sb.Append("*");
        }

        // FROM clause
        sb.Append($" FROM \"{_schemaName}\".\"{_tableName}\"");

        // JOIN clauses
        foreach (var join in _joinClauses)
        {
            var joinType = join.Type switch
            {
                JoinType.Inner => "INNER JOIN",
                JoinType.Left => "LEFT JOIN",
                JoinType.Right => "RIGHT JOIN",
                JoinType.Full => "FULL OUTER JOIN",
                _ => "INNER JOIN"
            };
            sb.Append($" {joinType} \"{join.Table}\" ON {join.Condition}");
        }

        // WHERE clause
        if (_whereConditions.Count > 0)
        {
            sb.Append(" WHERE ");
            var conditions = new List<string>();
            for (int i = 0; i < _whereConditions.Count; i++)
            {
                var condition = _whereConditions[i];
                var prefix = i > 0 ? $" {condition.LogicalOperator} " : "";

                if (condition.Operator.Equals("IN", StringComparison.OrdinalIgnoreCase))
                {
                    if (condition.Value is object[] values)
                    {
                        var placeholders = string.Join(", ", values.Select((_, idx) => $"@p{i}_{idx}"));
                        conditions.Add($"{prefix}\"{condition.Column}\" IN ({placeholders})");
                    }
                }
                else if (condition.Operator.Equals("BETWEEN", StringComparison.OrdinalIgnoreCase))
                {
                    conditions.Add($"{prefix}\"{condition.Column}\" BETWEEN @p{i}_0 AND @p{i}_1");
                }
                else
                {
                    conditions.Add($"{prefix}\"{condition.Column}\" {condition.Operator} @p{i}");
                }
            }
            sb.Append(string.Join("", conditions));
        }

        // GROUP BY clause
        if (_groupByColumns.Count > 0)
        {
            sb.Append($" GROUP BY {string.Join(", ", _groupByColumns.Select(c => $"\"{c}\""))}");
        }

        // ORDER BY clause
        if (_orderByClauses.Count > 0)
        {
            sb.Append(" ORDER BY ");
            var orders = _orderByClauses.Select(o =>
                $"\"{o.Column}\" {(o.Order == SortOrder.Desc ? "DESC" : "ASC")}");
            sb.Append(string.Join(", ", orders));
        }

        // LIMIT clause
        if (_limit.HasValue)
        {
            sb.Append($" LIMIT {_limit.Value}");
        }

        // OFFSET clause
        if (_offset.HasValue)
        {
            sb.Append($" OFFSET {_offset.Value}");
        }

        return sb.ToString();
    }

    private string BuildCountSql()
    {
        var sb = new StringBuilder();
        sb.Append("SELECT COUNT(*) FROM \"" + _schemaName + "\".\"" + _tableName + "\"");

        // JOIN clauses
        foreach (var join in _joinClauses)
        {
            var joinType = join.Type switch
            {
                JoinType.Inner => "INNER JOIN",
                JoinType.Left => "LEFT JOIN",
                JoinType.Right => "RIGHT JOIN",
                JoinType.Full => "FULL OUTER JOIN",
                _ => "INNER JOIN"
            };
            sb.Append($" {joinType} \"{join.Table}\" ON {join.Condition}");
        }

        // WHERE clause
        if (_whereConditions.Count > 0)
        {
            sb.Append(" WHERE ");
            var conditions = new List<string>();
            for (int i = 0; i < _whereConditions.Count; i++)
            {
                var condition = _whereConditions[i];
                var prefix = i > 0 ? $" {condition.LogicalOperator} " : "";

                if (condition.Operator.Equals("IN", StringComparison.OrdinalIgnoreCase))
                {
                    if (condition.Value is object[] values)
                    {
                        var placeholders = string.Join(", ", values.Select((_, idx) => $"@p{i}_{idx}"));
                        conditions.Add($"{prefix}\"{condition.Column}\" IN ({placeholders})");
                    }
                }
                else if (condition.Operator.Equals("BETWEEN", StringComparison.OrdinalIgnoreCase))
                {
                    conditions.Add($"{prefix}\"{condition.Column}\" BETWEEN @p{i}_0 AND @p{i}_1");
                }
                else
                {
                    conditions.Add($"{prefix}\"{condition.Column}\" {condition.Operator} @p{i}");
                }
            }
            sb.Append(string.Join("", conditions));
        }

        return sb.ToString();
    }

    private void AddParametersToCommand(NpgsqlCommand cmd)
    {
        for (int i = 0; i < _whereConditions.Count; i++)
        {
            var condition = _whereConditions[i];

            if (condition.Operator.Equals("IN", StringComparison.OrdinalIgnoreCase))
            {
                if (condition.Value is object[] values)
                {
                    for (int j = 0; j < values.Length; j++)
                    {
                        cmd.Parameters.AddWithValue($"@p{i}_{j}", values[j] ?? DBNull.Value);
                    }
                }
            }
            else if (condition.Operator.Equals("BETWEEN", StringComparison.OrdinalIgnoreCase))
            {
                if (condition.Value is object[] values && values.Length >= 2)
                {
                    cmd.Parameters.AddWithValue($"@p{i}_0", values[0] ?? DBNull.Value);
                    cmd.Parameters.AddWithValue($"@p{i}_1", values[1] ?? DBNull.Value);
                }
            }
            else
            {
                cmd.Parameters.AddWithValue($"@p{i}", condition.Value ?? DBNull.Value);
            }
        }
    }

    private T MapReaderToEntity(NpgsqlDataReader reader)
    {
        var entity = new T();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<IgnoreAttribute>() != null)
                continue;

            var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
            var columnName = columnAttr?.Name ?? property.Name;

            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                {
                    property.SetValue(entity, null);
                    continue;
                }

                var value = reader.GetValue(ordinal);
                var dataType = columnAttr?.DataType ?? DataType.VarChar;
                var convertedValue = TypeConverter.FromPostgreSQL(value, dataType, property.PropertyType);
                property.SetValue(entity, convertedValue);
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to map column {columnName}: {ex.Message}");
            }
        }

        return entity;
    }
}

// Supporting Classes for QueryBuilder

/// <summary>
/// Represents an ORDER BY clause.
/// </summary>
public class OrderByClause
{
    public required string Column { get; set; }
    public SortOrder Order { get; set; } = SortOrder.Asc;
}

/// <summary>
/// Represents a JOIN clause.
/// </summary>
public class JoinClause
{
    public JoinType Type { get; set; } = JoinType.Inner;
    public required string Table { get; set; }
    public required string Condition { get; set; }
}

/// <summary>
/// Represents an aggregate function.
/// </summary>
public class AggregateFunction
{
    public AggregateType Type { get; set; }
    public required string Column { get; set; }
    public required string Alias { get; set; }
}

/// <summary>
/// Enumerates the types of JOINs.
/// </summary>
public enum JoinType
{
    Inner,
    Left,
    Right,
    Full
}

/// <summary>
/// Enumerates aggregate function types.
/// </summary>
public enum AggregateType
{
    Count,
    Sum,
    Avg,
    Min,
    Max
}

/// <summary>
/// Non-generic query builder factory for PostgreSQL.
/// </summary>
public class QueryBuilder
{
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger? _logger;

    public QueryBuilder(ConnectionManager connectionManager, ILogger? logger = null)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger;
    }

    /// <summary>
    /// Creates a typed query builder for the specified model type.
    /// </summary>
    public QueryBuilder<T> For<T>(string connectionId = "Default") where T : class, new()
    {
        return new QueryBuilder<T>(_connectionManager, _logger, connectionId);
    }
}
