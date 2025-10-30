using CodeLogic.Abstractions;
using CL.PostgreSQL.Core;
using CL.PostgreSQL.Models;
using Npgsql;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CL.PostgreSQL.Services;

/// <summary>
/// Type-safe, LINQ-based fluent query builder for constructing complex SQL queries.
/// Provides compile-time safety with Expression Trees and IntelliSense support.
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
    /// Specifies which columns to select using a LINQ expression.
    /// </summary>
    public QueryBuilder<T> Select(Expression<Func<T, object?>> columns)
    {
        var selectedColumns = ExpressionVisitor.ParseSelect(columns);
        _selectColumns.AddRange(selectedColumns);
        return this;
    }

    /// <summary>
    /// Adds a WHERE condition using a LINQ expression (type-safe!).
    /// </summary>
    public QueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        var conditions = ExpressionVisitor.Parse(predicate);
        _whereConditions.AddRange(conditions);
        return this;
    }

    /// <summary>
    /// Adds multiple WHERE conditions (chainable).
    /// </summary>
    public QueryBuilder<T> Where(Expression<Func<T, bool>> predicate1, Expression<Func<T, bool>> predicate2)
    {
        Where(predicate1);
        Where(predicate2);
        return this;
    }

    /// <summary>
    /// Orders results by a property (ascending).
    /// </summary>
    public QueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var (column, _) = ExpressionVisitor.ParseOrderBy(keySelector, descending: false);
        _orderByClauses.Add(new OrderByClause { Column = column, Order = SortOrder.Asc });
        return this;
    }

    /// <summary>
    /// Orders results by a property (descending).
    /// </summary>
    public QueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var (column, _) = ExpressionVisitor.ParseOrderBy(keySelector, descending: true);
        _orderByClauses.Add(new OrderByClause { Column = column, Order = SortOrder.Desc });
        return this;
    }

    /// <summary>
    /// Then orders results by another property (ascending).
    /// </summary>
    public QueryBuilder<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        return OrderBy(keySelector);
    }

    /// <summary>
    /// Then orders results by another property (descending).
    /// </summary>
    public QueryBuilder<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        return OrderByDescending(keySelector);
    }

    /// <summary>
    /// Groups results by a property.
    /// </summary>
    public QueryBuilder<T> GroupBy(Expression<Func<T, object?>> keySelector)
    {
        var columns = ExpressionVisitor.ParseGroupBy(keySelector);
        _groupByColumns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Adds a JOIN clause.
    /// </summary>
    public QueryBuilder<T> Join(string table, Expression<Func<T, bool>> condition, JoinType joinType = JoinType.Inner)
    {
        var conditionStr = condition.ToString(); // Simplified - in production use condition expression properly
        _joinClauses.Add(new JoinClause
        {
            Type = joinType,
            Table = table,
            Condition = conditionStr
        });
        return this;
    }

    /// <summary>
    /// Adds an INNER JOIN clause.
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
    /// Adds a COUNT aggregate.
    /// </summary>
    public QueryBuilder<T> Count(string alias = "count")
    {
        _aggregateFunctions.Add(new AggregateFunction
        {
            Type = AggregateType.Count,
            Column = "*",
            Alias = alias
        });
        return this;
    }

    /// <summary>
    /// Adds a SUM aggregate for a property.
    /// </summary>
    public QueryBuilder<T> Sum<TKey>(Expression<Func<T, TKey>> column, string alias = "sum")
    {
        var (columnName, _) = ExpressionVisitor.ParseOrderBy(column);
        _aggregateFunctions.Add(new AggregateFunction
        {
            Type = AggregateType.Sum,
            Column = columnName,
            Alias = alias
        });
        return this;
    }

    /// <summary>
    /// Adds an AVG aggregate for a property.
    /// </summary>
    public QueryBuilder<T> Avg<TKey>(Expression<Func<T, TKey>> column, string alias = "avg")
    {
        var (columnName, _) = ExpressionVisitor.ParseOrderBy(column);
        _aggregateFunctions.Add(new AggregateFunction
        {
            Type = AggregateType.Avg,
            Column = columnName,
            Alias = alias
        });
        return this;
    }

    /// <summary>
    /// Adds a MIN aggregate for a property.
    /// </summary>
    public QueryBuilder<T> Min<TKey>(Expression<Func<T, TKey>> column, string alias = "min")
    {
        var (columnName, _) = ExpressionVisitor.ParseOrderBy(column);
        _aggregateFunctions.Add(new AggregateFunction
        {
            Type = AggregateType.Min,
            Column = columnName,
            Alias = alias
        });
        return this;
    }

    /// <summary>
    /// Adds a MAX aggregate for a property.
    /// </summary>
    public QueryBuilder<T> Max<TKey>(Expression<Func<T, TKey>> column, string alias = "max")
    {
        var (columnName, _) = ExpressionVisitor.ParseOrderBy(column);
        _aggregateFunctions.Add(new AggregateFunction
        {
            Type = AggregateType.Max,
            Column = columnName,
            Alias = alias
        });
        return this;
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
    public QueryBuilder<T> Skip(int count)
    {
        _offset = count;
        return this;
    }

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
    public async Task<T?> FirstAsync(CancellationToken cancellationToken = default)
    {
        var results = await ExecuteAsync(cancellationToken);
        return results.FirstOrDefault();
    }

    /// <summary>
    /// Executes the query and returns the first result or null.
    /// </summary>
    public async Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await FirstAsync(cancellationToken);
    }

    /// <summary>
    /// Executes the query and returns paginated results.
    /// </summary>
    public async Task<PagedResult<T>> ToPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
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
                else if (condition.Operator.Equals("IS", StringComparison.OrdinalIgnoreCase) ||
                         condition.Operator.Equals("IS NOT", StringComparison.OrdinalIgnoreCase))
                {
                    conditions.Add($"{prefix}\"{condition.Column}\" {condition.Operator} NULL");
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
                else if (condition.Operator.Equals("IS", StringComparison.OrdinalIgnoreCase) ||
                         condition.Operator.Equals("IS NOT", StringComparison.OrdinalIgnoreCase))
                {
                    conditions.Add($"{prefix}\"{condition.Column}\" {condition.Operator} NULL");
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
            else if (condition.Operator.Equals("IS", StringComparison.OrdinalIgnoreCase) ||
                     condition.Operator.Equals("IS NOT", StringComparison.OrdinalIgnoreCase))
            {
                // No parameters needed for NULL checks
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
