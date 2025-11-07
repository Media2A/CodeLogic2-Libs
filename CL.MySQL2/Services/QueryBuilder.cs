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
    private readonly List<LambdaExpression> _includeExpressions = new();
    private readonly List<AggregateFunction> _aggregateFunctions = new();
    private readonly List<string> _groupByColumns = new();
    private int? _limit;
    private int? _offset;
    private string _connectionId = "Default";
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger? _logger;
    private readonly TransactionScope? _transactionScope;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryBuilder{T}"/> class.
    /// </summary>
    /// <param name="connectionManager">The connection manager for database access.</param>
    /// <param name="logger">The logger for recording operations and errors.</param>
    /// <param name="connectionId">The ID of the connection to use.</param>
    public QueryBuilder(ConnectionManager connectionManager, ILogger? logger = null, string connectionId = "Default")
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger;
        _connectionId = connectionId;

        var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>();
        _tableName = tableAttr?.Name ?? typeof(T).Name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryBuilder{T}"/> class to operate within a transaction.
    /// </summary>
    /// <param name="connectionManager">The connection manager for database access.</param>
    /// <param name="logger">The logger for recording operations and errors.</param>
    /// <param name="transactionScope">The transaction scope to use for all operations.</param>
    public QueryBuilder(ConnectionManager connectionManager, ILogger? logger, TransactionScope transactionScope)
        : this(connectionManager, logger, transactionScope.ConnectionId)
    {
        _transactionScope = transactionScope;
    }

    private async Task<TResult> ExecuteDbOperationAsync<TResult>(Func<MySqlConnection, MySqlTransaction?, Task<TResult>> operation)
    {
        if (_transactionScope != null)
        {
            return await operation(_transactionScope.Connection, _transactionScope.Transaction);
        }
        else
        {
            return await _connectionManager.ExecuteWithConnectionAsync(
                async (connection) => await operation(connection, null),
                _connectionId
            );
        }
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
    /// Eager-loads a related entity. (Currently only supports one-to-many relationships).
    /// Example: .Include(p => p.Posts)
    /// </summary>
    public QueryBuilder<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath)
    {
        _includeExpressions.Add(navigationPropertyPath);
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

            return await ExecuteDbOperationAsync(async (connection, transaction) =>
            {
                using var cmd = new MySqlCommand(sql, connection, transaction);
                AddParametersToCommand(cmd);

                if (config.EnableLogging)
                {
                    _logger?.Debug($"Executing query: {sql}");
                }

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                var mainEntities = new Dictionary<object, T>();
                var pkProperty = typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Primary == true);

                if (pkProperty == null)
                {
                    throw new InvalidOperationException($"No primary key defined for type {typeof(T).Name}");
                }

                while (await reader.ReadAsync(cancellationToken))
                {
                    // Determine if we should use prefixed aliases
                    bool usePrefixedAliases = _includeExpressions.Any();

                    // Get primary key value
                    var pkColumnName = usePrefixedAliases
                        ? $"{_tableName}_{pkProperty.Name}"
                        : (pkProperty.GetCustomAttribute<ColumnAttribute>()?.Name ?? pkProperty.Name);
                    var pkValue = reader[pkColumnName];

                    if (pkValue == DBNull.Value) continue;

                    if (!mainEntities.TryGetValue(pkValue, out var mainEntity))
                    {
                        mainEntity = new T();
                        // Map main entity properties
                        foreach (var prop in typeof(T).GetProperties().Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null))
                        {
                            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
                            if (colAttr != null)
                            {
                                // Use prefixed alias if includes are present, otherwise use actual column name
                                var columnName = usePrefixedAliases
                                    ? $"{_tableName}_{prop.Name}"
                                    : (colAttr.Name ?? prop.Name);

                                if (reader.HasColumn(columnName) && reader[columnName] != DBNull.Value)
                                {
                                    var convertedValue = TypeConverter.FromMySql(reader[columnName], colAttr.DataType, prop.PropertyType);
                                    prop.SetValue(mainEntity, convertedValue);
                                }
                            }
                        }
                        mainEntities[pkValue] = mainEntity;
                    }

                    // Handle included navigation properties
                    foreach (var include in _includeExpressions)
                    {
                        var memberExpr = include.Body as MemberExpression;
                        if (memberExpr == null) continue;

                        var navProperty = memberExpr.Member as PropertyInfo;
                        if (navProperty == null) continue;

                        var childType = navProperty.PropertyType.GetGenericArguments()[0];
                        var childTableAttr = childType.GetCustomAttribute<TableAttribute>();
                        var childTableName = childTableAttr?.Name ?? childType.Name;
                        var childPkProp = childType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Primary == true);
                        if (childPkProp == null) continue;

                        var childPkAlias = $"{childTableName}_{childPkProp.Name}";
                        if (!reader.HasColumn(childPkAlias) || reader[childPkAlias] == DBNull.Value) continue;

                        var childPkValue = reader[childPkAlias];
                        var collection = navProperty.GetValue(mainEntity) as System.Collections.ICollection;
                        if (collection == null)
                        {
                            var listType = typeof(List<>).MakeGenericType(childType);
                            collection = (System.Collections.ICollection)Activator.CreateInstance(listType);
                            navProperty.SetValue(mainEntity, collection);
                        }

                        // Check if child entity is already in the collection
                        bool exists = false;
                        foreach (var item in collection)
                        {
                            if (childPkProp.GetValue(item).Equals(childPkValue))
                            {
                                exists = true;
                                break;
                            }
                        }

                        if (!exists)
                        {
                            var childEntity = Activator.CreateInstance(childType);
                            foreach (var prop in childType.GetProperties().Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null))
                            {
                                var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
                                if (colAttr != null)
                                {
                                    var alias = $"{childTableName}_{prop.Name}";
                                    if (reader.HasColumn(alias) && reader[alias] != DBNull.Value)
                                    {
                                        var convertedValue = TypeConverter.FromMySql(reader[alias], colAttr.DataType, prop.PropertyType);
                                        prop.SetValue(childEntity, convertedValue);
                                    }
                                }
                            }
                            collection.GetType().GetMethod("Add").Invoke(collection, new[] { childEntity });
                        }
                    }
                }

                var duration = DateTime.UtcNow - startTime;
                if (config.LogSlowQueries && duration.TotalMilliseconds > config.SlowQueryThreshold)
                {
                    _logger?.Warning($"Slow query detected ({duration.TotalMilliseconds}ms): {sql}");
                }

                return OperationResult<List<T>>.Ok(mainEntities.Values.ToList());
            });
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
            var countResult = await CountAsync(cancellationToken);
            if (!countResult.Success) 
            {
                return OperationResult<PagedResult<T>>.Fail(countResult.ErrorMessage, countResult.Exception);
            }
            var totalItems = countResult.Data;

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

            return await ExecuteDbOperationAsync(async (connection, transaction) =>
            {
                using var cmd = new MySqlCommand(countQuery, connection, transaction);
                AddParametersToCommand(cmd);

                var count = Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken));
                return OperationResult<long>.Ok(count);
            });
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

            return await ExecuteDbOperationAsync(async (connection, transaction) =>
            {
                using var cmd = new MySqlCommand(deleteQuery, connection, transaction);
                AddParametersToCommand(cmd);

                var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return OperationResult<int>.Ok(rowsAffected);
            });
        }
        catch (Exception ex)
        {
            _logger?.Error($"Delete query failed: {ex.Message}", ex);
            return OperationResult<int>.Fail($"Delete query failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executes an UPDATE query with WHERE conditions and returns the number of rows affected.
    /// Example: queryBuilder.Where(u => u.IsActive == false).UpdateAsync(new { IsActive = true, LastModified = DateTime.UtcNow })
    /// </summary>
    public async Task<OperationResult<int>> UpdateAsync(object updateValues, CancellationToken cancellationToken = default)
    {
        if (!_whereConditions.Any())
        {
            return OperationResult<int>.Fail("UPDATE operation without a WHERE clause is not allowed. Please specify a WHERE condition.");
        }

        try
        {
            var updateQuery = BuildUpdateQuery(updateValues, out var updateParameters);

            return await ExecuteDbOperationAsync(async (connection, transaction) =>
            {
                using var cmd = new MySqlCommand(updateQuery, connection, transaction);
                
                // Add parameters for the SET clause
                foreach (var param in updateParameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }

                // Add parameters for the WHERE clause
                AddParametersToCommand(cmd);

                var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
                return OperationResult<int>.Ok(rowsAffected);
            });
        }
        catch (Exception ex)
        {
            _logger?.Error($"Update query failed: {ex.Message}", ex);
            return OperationResult<int>.Fail($"Update query failed: {ex.Message}", ex);
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
        var includedNavigations = new List<(PropertyInfo, Type, string)>();

        // Handle includes by generating JOINs
        foreach (var include in _includeExpressions)
        {
            var memberExpr = include.Body as MemberExpression;
            if (memberExpr == null) continue;

            var navProperty = memberExpr.Member as PropertyInfo;
            if (navProperty == null) continue;

            var parentType = typeof(T);
            var parentTableName = _tableName;

            var m2mAttr = navProperty.GetCustomAttribute<ManyToManyAttribute>();

            // Many-to-Many
            if (m2mAttr != null)
            {
                var childType = navProperty.PropertyType.GetGenericArguments()[0];
                var childTableAttr = childType.GetCustomAttribute<TableAttribute>();
                var childTableName = childTableAttr?.Name ?? childType.Name;

                var junctionType = m2mAttr.JunctionEntityType;
                var junctionTableAttr = junctionType.GetCustomAttribute<TableAttribute>();
                var junctionTableName = junctionTableAttr?.Name ?? junctionType.Name;

                // Join 1: Parent -> Junction
                var parentPkProp = parentType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Primary == true);
                var parentPkName = parentPkProp.GetCustomAttribute<ColumnAttribute>()?.Name ?? parentPkProp.Name;
                var fkToParent = junctionType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute<ForeignKeyAttribute>()?.ReferenceTable.Equals(parentTableName, StringComparison.OrdinalIgnoreCase) == true);
                var fkToParentName = fkToParent.GetCustomAttribute<ColumnAttribute>()?.Name ?? fkToParent.Name;
                _joinClauses.Add(new JoinClause { Type = JoinType.Left, Table = junctionTableName, Condition = $"`{parentTableName}`.`{parentPkName}` = `{junctionTableName}`.`{fkToParentName}`" });

                // Join 2: Junction -> Child
                var childPkProp = childType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Primary == true);
                var childPkName = childPkProp.GetCustomAttribute<ColumnAttribute>()?.Name ?? childPkProp.Name;
                var fkToChild = junctionType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute<ForeignKeyAttribute>()?.ReferenceTable.Equals(childTableName, StringComparison.OrdinalIgnoreCase) == true);
                var fkToChildName = fkToChild.GetCustomAttribute<ColumnAttribute>()?.Name ?? fkToChild.Name;
                _joinClauses.Add(new JoinClause { Type = JoinType.Left, Table = childTableName, Condition = $"`{junctionTableName}`.`{fkToChildName}` = `{childTableName}`.`{childPkName}`" });

                includedNavigations.Add((navProperty, childType, childTableName));
            }
            // One-to-Many or Many-to-One
            else
            {
                var childType = navProperty.PropertyType;
                // One-to-many
                if (childType.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(childType))
                {
                    childType = childType.GetGenericArguments()[0];
                    var childTableAttr = childType.GetCustomAttribute<TableAttribute>();
                    var childTableName = childTableAttr?.Name ?? childType.Name;

                    var foreignKeyProp = childType.GetProperties().FirstOrDefault(p =>
                        p.GetCustomAttribute<ForeignKeyAttribute>()?.ReferenceTable.Equals(parentTableName, StringComparison.OrdinalIgnoreCase) == true ||
                        p.GetCustomAttribute<ForeignKeyAttribute>()?.ReferenceTable.Equals(parentType.Name, StringComparison.OrdinalIgnoreCase) == true
                    );

                    if (foreignKeyProp != null)
                    {
                        var parentPkProp = parentType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Primary == true);
                        var parentPkName = parentPkProp?.GetCustomAttribute<ColumnAttribute>()?.Name ?? parentPkProp?.Name ?? "id";
                        var childFkName = foreignKeyProp.GetCustomAttribute<ColumnAttribute>()?.Name ?? foreignKeyProp.Name;

                        _joinClauses.Add(new JoinClause { Type = JoinType.Left, Table = childTableName, Condition = $"`{parentTableName}`.`{parentPkName}` = `{childTableName}`.`{childFkName}`" });
                        includedNavigations.Add((navProperty, childType, childTableName));
                    }
                }
                // Many-to-one
                else
                {
                    var fkAttr = navProperty.GetCustomAttribute<ForeignKeyAttribute>();
                    if (fkAttr != null)
                    {
                        var referencedTable = fkAttr.ReferenceTable;
                        var referencedColumn = fkAttr.ReferenceColumn;
                        var localColumn = navProperty.GetCustomAttribute<ColumnAttribute>()?.Name ?? navProperty.Name;

                        _joinClauses.Add(new JoinClause { Type = JoinType.Left, Table = referencedTable, Condition = $"`{parentTableName}`.`{localColumn}` = `{referencedTable}`.`{referencedColumn}`" });
                        includedNavigations.Add((navProperty, childType, referencedTable));
                    }
                }
            }
        }

        var sb = new StringBuilder();
        sb.Append("SELECT ");

        if (_aggregateFunctions.Any())
        {
            var aggParts = _aggregateFunctions.Select(a =>
                $"{a.Type.ToString().ToUpper()}(`{a.Column}`) AS `{a.Alias}`");

            if (_selectColumns.Any())
            {
                sb.Append($"{string.Join(", ", _selectColumns.Select(c => $"`{c}`"))}, {string.Join(", ", aggParts)}");
            }
            else
            {
                sb.Append(string.Join(", ", aggParts));
            }
        }
        else if (_includeExpressions.Any())
        {
            var allSelectColumns = new List<string>();
            var baseProps = typeof(T).GetProperties().Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null);
            foreach (var prop in baseProps)
            {
                var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
                if (colAttr != null && prop.GetCustomAttribute<ForeignKeyAttribute>() == null)
                {
                    allSelectColumns.Add($"`{_tableName}`.`{colAttr.Name ?? prop.Name}` AS `{_tableName}_{prop.Name}`");
                }
            }

            foreach (var (navProp, type, tableName) in includedNavigations)
            {
                var includedProps = type.GetProperties().Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null);
                foreach (var prop in includedProps)
                {
                    var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
                    if (colAttr != null)
                    {
                        allSelectColumns.Add($"`{tableName}`.`{colAttr.Name ?? prop.Name}` AS `{tableName}_{prop.Name}`");
                    }
                }
            }
            sb.Append(string.Join(", ", allSelectColumns.Distinct()));
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

    private string BuildUpdateQuery(object updateValues, out Dictionary<string, object> parameters)
    {
        parameters = new Dictionary<string, object>();
        var sb = new StringBuilder();
        sb.Append($"UPDATE `{_tableName}` SET ");

        var setClauses = new List<string>();
        var updateProps = updateValues.GetType().GetProperties();
        foreach (var prop in updateProps)
        {
            var paramName = $"@set_{prop.Name}";
            setClauses.Add($"`{prop.Name}` = {paramName}");
            parameters[paramName] = prop.GetValue(updateValues);
        }
        sb.Append(string.Join(", ", setClauses));

        // WHERE clause (reusing existing logic)
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


}

/// <summary>
/// Non-generic query builder factory for creating query builders for specific types.
/// </summary>
public class QueryBuilder
{
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger? _logger;
    private readonly string _connectionId;
    private readonly TransactionScope? _transactionScope;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryBuilder"/> class.
    /// </summary>
    /// <param name="connectionManager">The connection manager for database access.</param>
    /// <param name="logger">The logger for recording operations and errors.</param>
    /// <param name="connectionId">The ID of the connection to use.</param>
    public QueryBuilder(ConnectionManager connectionManager, ILogger? logger = null, string connectionId = "Default")
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger;
        _connectionId = connectionId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryBuilder"/> class to operate within a transaction.
    /// </summary>
    /// <param name="connectionManager">The connection manager for database access.</param>
    /// <param name="logger">The logger for recording operations and errors.</param>
    /// <param name="transactionScope">The transaction scope to use for all operations.</param>
    public QueryBuilder(ConnectionManager connectionManager, ILogger? logger, TransactionScope transactionScope)
        : this(connectionManager, logger, transactionScope.ConnectionId)
    {
        _transactionScope = transactionScope;
    }

    /// <summary>
    /// Creates a query builder for the specified model type.
    /// </summary>
    public QueryBuilder<T> For<T>() where T : class, new()
    {
        if (_transactionScope != null)
        {
            return new QueryBuilder<T>(_connectionManager, _logger, _transactionScope);
        }
        return new QueryBuilder<T>(_connectionManager, _logger, _connectionId);
    }
}

/// <summary>
/// Provides extension methods for <see cref="MySqlDataReader"/>.
/// </summary>
public static class MySqlDataReaderExtensions
{
    /// <summary>
    /// Checks if a column exists in the data reader.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="columnName">The name of the column to check.</param>
    /// <returns>True if the column exists, otherwise false.</returns>
    public static bool HasColumn(this MySqlDataReader reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
