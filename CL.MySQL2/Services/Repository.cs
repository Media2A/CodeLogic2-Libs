using CodeLogic.Abstractions;
using CL.MySQL2.Core;
using CL.MySQL2.Models;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace CL.MySQL2.Services;

/// <summary>
/// Generic repository for performing CRUD operations on model types.
/// Provides a high-level, type-safe interface for database operations.
/// </summary>
public class Repository<T> where T : class, new()
{
    private readonly string _connectionId;
    private readonly string _tableName;
    private readonly IMemoryCache _cache;
    private readonly DatabaseConfiguration _config;
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger? _logger;
    private static readonly ConcurrentDictionary<string, PropertyInfo[]> _propertyCache = new();

    public Repository(ConnectionManager connectionManager, ILogger? logger = null, string connectionId = "Default")
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger;
        _connectionId = connectionId;
        _config = _connectionManager.GetConfiguration(connectionId);

        var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>();
        _tableName = tableAttr?.Name ?? typeof(T).Name;

        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
    }

    public async Task<OperationResult<T>> InsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var (columns, values, parameters) = BuildInsertParameters(entity);
                var sql = $"INSERT INTO `{_tableName}` ({columns}) VALUES ({values}); SELECT LAST_INSERT_ID();";

                using var cmd = new MySqlCommand(sql, connection);
                foreach (var param in parameters)
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);

                var lastInsertId = await cmd.ExecuteScalarAsync(cancellationToken);
                SetPrimaryKeyValue(entity, lastInsertId);

                if (_config.EnableLogging)
                    _logger?.Debug($"Inserted record into {_tableName}");

                return OperationResult<T>.Ok(entity, 1);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to insert record into {_tableName}", ex);
            return OperationResult<T>.Fail($"Failed to insert record: {ex.Message}", ex);
        }
    }

    public async Task<OperationResult<T>> GetByIdAsync(object id, int cacheTtl = 0, CancellationToken cancellationToken = default)
    {
        try
        {
            var primaryKey = GetPrimaryKeyProperty();
            if (primaryKey == null)
                return OperationResult<T>.Fail("No primary key defined on the model");

            var columnName = primaryKey.GetCustomAttribute<ColumnAttribute>()?.Name ?? primaryKey.Name;
            return await GetByColumnAsync(columnName, id, cacheTtl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to retrieve record from {_tableName}", ex);
            return OperationResult<T>.Fail($"Failed to retrieve record: {ex.Message}", ex);
        }
    }

    public async Task<OperationResult<T>> GetByColumnAsync(string columnName, object value, int cacheTtl = 0, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"{_tableName}:{columnName}:{value}";

            if (cacheTtl > 0 && _config.EnableCaching && _cache.TryGetValue<T>(cacheKey, out var cached))
                return OperationResult<T>.Ok(cached);

            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var sql = $"SELECT * FROM `{_tableName}` WHERE `{columnName}` = @value LIMIT 1";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@value", value ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                if (await reader.ReadAsync(cancellationToken))
                {
                    var entity = MapReaderToEntity(reader);

                    if (cacheTtl > 0 && _config.EnableCaching)
                    {
                        _cache.Set(cacheKey, entity, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheTtl),
                            Size = 1
                        });
                    }

                    return OperationResult<T>.Ok(entity);
                }

                return OperationResult<T>.Ok(null);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to retrieve record from {_tableName}", ex);
            return OperationResult<T>.Fail($"Failed to retrieve record: {ex.Message}", ex);
        }
    }

    public async Task<OperationResult<List<T>>> GetAllAsync(int cacheTtl = 0, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"{_tableName}:all";

            if (cacheTtl > 0 && _config.EnableCaching && _cache.TryGetValue<List<T>>(cacheKey, out var cached))
                return OperationResult<List<T>>.Ok(cached);

            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var sql = $"SELECT * FROM `{_tableName}`";
                using var cmd = new MySqlCommand(sql, connection);
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                var entities = new List<T>();
                while (await reader.ReadAsync(cancellationToken))
                    entities.Add(MapReaderToEntity(reader));

                if (cacheTtl > 0 && _config.EnableCaching)
                {
                    _cache.Set(cacheKey, entities, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheTtl),
                        Size = entities.Count
                    });
                }

                return OperationResult<List<T>>.Ok(entities);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to retrieve records from {_tableName}", ex);
            return OperationResult<List<T>>.Fail($"Failed to retrieve records: {ex.Message}", ex);
        }
    }

    public async Task<OperationResult<PagedResult<T>>> GetPagedAsync(int page, int pageSize, int cacheTtl = 0, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"{_tableName}:paged:{page}:{pageSize}";

            if (cacheTtl > 0 && _config.EnableCaching && _cache.TryGetValue<PagedResult<T>>(cacheKey, out var cached))
                return OperationResult<PagedResult<T>>.Ok(cached);

            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var totalItems = (await CountAsync(cancellationToken)).Data;

                var offset = (page - 1) * pageSize;
                var sql = $"SELECT * FROM `{_tableName}` LIMIT @pageSize OFFSET @offset";
                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@pageSize", pageSize);
                cmd.Parameters.AddWithValue("@offset", offset);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                var entities = new List<T>();
                while (await reader.ReadAsync(cancellationToken))
                    entities.Add(MapReaderToEntity(reader));

                var pagedResult = new PagedResult<T>
                {
                    Items = entities,
                    TotalItems = totalItems,
                    PageNumber = page,
                    PageSize = pageSize
                };

                if (cacheTtl > 0 && _config.EnableCaching)
                {
                    _cache.Set(cacheKey, pagedResult, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheTtl),
                        Size = entities.Count
                    });
                }

                return OperationResult<PagedResult<T>>.Ok(pagedResult);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to retrieve paged records from {_tableName}", ex);
            return OperationResult<PagedResult<T>>.Fail($"Failed to retrieve paged records: {ex.Message}", ex);
        }
    }

    public async Task<OperationResult<int>> CountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var sql = $"SELECT COUNT(*) FROM `{_tableName}`";
                using var cmd = new MySqlCommand(sql, connection);
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                var count = Convert.ToInt32(result);
                return OperationResult<int>.Ok(count);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to count records in {_tableName}", ex);
            return OperationResult<int>.Fail($"Failed to count records: {ex.Message}", ex);
        }
    }


    public async Task<OperationResult<T>> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var primaryKey = GetPrimaryKeyProperty();
                if (primaryKey == null)
                    return OperationResult<T>.Fail("No primary key defined on the model");

                var pkColumnName = primaryKey.GetCustomAttribute<ColumnAttribute>()?.Name ?? primaryKey.Name;
                var pkValue = primaryKey.GetValue(entity);

                // Update fields that have OnUpdateCurrentTimestamp attribute
                foreach (var prop in GetCachedProperties())
                {
                    var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                    if (columnAttr != null && columnAttr.OnUpdateCurrentTimestamp && prop.PropertyType == typeof(DateTime))
                    {
                        prop.SetValue(entity, DateTime.Now);
                    }
                }

                var (setClause, parameters) = BuildUpdateParameters(entity);
                var sql = $"UPDATE `{_tableName}` SET {setClause} WHERE `{pkColumnName}` = @__pk__";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@__pk__", pkValue ?? DBNull.Value);

                foreach (var param in parameters)
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);

                var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);

                if (_config.EnableLogging)
                    _logger?.Debug($"Updated {rowsAffected} record(s) in {_tableName}");

                return OperationResult<T>.Ok(entity, rowsAffected);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to update record in {_tableName}", ex);
            return OperationResult<T>.Fail($"Failed to update record: {ex.Message}", ex);
        }
    }

    public async Task<OperationResult<int>> DeleteAsync(object id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var primaryKey = GetPrimaryKeyProperty();
                if (primaryKey == null)
                    return OperationResult<int>.Fail("No primary key defined on the model");

                var columnName = primaryKey.GetCustomAttribute<ColumnAttribute>()?.Name ?? primaryKey.Name;
                var sql = $"DELETE FROM `{_tableName}` WHERE `{columnName}` = @id";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", id ?? DBNull.Value);

                var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);

                if (_config.EnableLogging)
                    _logger?.Debug($"Deleted {rowsAffected} record(s) from {_tableName}");

                return OperationResult<int>.Ok(rowsAffected, rowsAffected);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to delete record from {_tableName}", ex);
            return OperationResult<int>.Fail($"Failed to delete record: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Increments a numeric column by the specified amount.
    /// Example: repository.IncrementAsync(postId, p => p.ViewCount, 1)
    /// </summary>
    public async Task<OperationResult<int>> IncrementAsync<TProperty>(object id, Expression<Func<T, TProperty>> columnSelector, int amount = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var primaryKey = GetPrimaryKeyProperty();
                if (primaryKey == null)
                    return OperationResult<int>.Fail("No primary key defined on the model");

                var pkColumnName = primaryKey.GetCustomAttribute<ColumnAttribute>()?.Name ?? primaryKey.Name;

                // Get the column name from the expression
                var memberExpr = columnSelector.Body as MemberExpression;
                if (memberExpr == null)
                    return OperationResult<int>.Fail("Invalid column selector expression");

                var property = memberExpr.Member as PropertyInfo;
                if (property == null)
                    return OperationResult<int>.Fail("Invalid column selector expression");

                var columnName = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name;

                var sql = $"UPDATE `{_tableName}` SET `{columnName}` = `{columnName}` + @amount WHERE `{pkColumnName}` = @id";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@id", id ?? DBNull.Value);

                var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);

                if (_config.EnableLogging)
                    _logger?.Debug($"Incremented {columnName} by {amount} in {_tableName}");

                return OperationResult<int>.Ok(rowsAffected);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to increment column in {_tableName}", ex);
            return OperationResult<int>.Fail($"Failed to increment column: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Decrements a numeric column by the specified amount.
    /// Example: repository.DecrementAsync(postId, p => p.CommentCount, 1)
    /// Optionally uses GREATEST to ensure the value doesn't go below zero.
    /// </summary>
    public async Task<OperationResult<int>> DecrementAsync<TProperty>(object id, Expression<Func<T, TProperty>> columnSelector, int amount = 1, bool preventNegative = true, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var primaryKey = GetPrimaryKeyProperty();
                if (primaryKey == null)
                    return OperationResult<int>.Fail("No primary key defined on the model");

                var pkColumnName = primaryKey.GetCustomAttribute<ColumnAttribute>()?.Name ?? primaryKey.Name;

                // Get the column name from the expression
                var memberExpr = columnSelector.Body as MemberExpression;
                if (memberExpr == null)
                    return OperationResult<int>.Fail("Invalid column selector expression");

                var property = memberExpr.Member as PropertyInfo;
                if (property == null)
                    return OperationResult<int>.Fail("Invalid column selector expression");

                var columnName = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name;

                string sql;
                if (preventNegative)
                {
                    sql = $"UPDATE `{_tableName}` SET `{columnName}` = GREATEST(`{columnName}` - @amount, 0) WHERE `{pkColumnName}` = @id";
                }
                else
                {
                    sql = $"UPDATE `{_tableName}` SET `{columnName}` = `{columnName}` - @amount WHERE `{pkColumnName}` = @id";
                }

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@id", id ?? DBNull.Value);

                var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);

                if (_config.EnableLogging)
                    _logger?.Debug($"Decremented {columnName} by {amount} in {_tableName}");

                return OperationResult<int>.Ok(rowsAffected);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to decrement column in {_tableName}", ex);
            return OperationResult<int>.Fail($"Failed to decrement column: {ex.Message}", ex);
        }
    }


    public async Task<OperationResult<PagedResult<T>>> FindAsync(IEnumerable<WhereCondition> conditions, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var whereClauses = new List<string>();
                var parameters = new Dictionary<string, object>();

                foreach (var condition in conditions)
                {
                    whereClauses.Add($"`{condition.Column}` {condition.Operator} @{condition.Column}");
                    parameters.Add($"@{condition.Column}", condition.Value);
                }

                var whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                var countSql = $"SELECT COUNT(*) FROM `{_tableName}` {whereSql}";
                using var countCmd = new MySqlCommand(countSql, connection);
                foreach (var param in parameters)
                {
                    countCmd.Parameters.AddWithValue(param.Key, param.Value);
                }
                var totalItems = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));

                var offset = (page - 1) * pageSize;
                var sql = $"SELECT * FROM `{_tableName}` {whereSql} LIMIT @pageSize OFFSET @offset";
                using var cmd = new MySqlCommand(sql, connection);
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
                }
                cmd.Parameters.AddWithValue("@pageSize", pageSize);
                cmd.Parameters.AddWithValue("@offset", offset);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                var entities = new List<T>();
                while (await reader.ReadAsync(cancellationToken))
                    entities.Add(MapReaderToEntity(reader));

                var pagedResult = new PagedResult<T>
                {
                    Items = entities,
                    TotalItems = totalItems,
                    PageNumber = page,
                    PageSize = pageSize
                };

                return OperationResult<PagedResult<T>>.Ok(pagedResult);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to find records in {_tableName}", ex);
            return OperationResult<PagedResult<T>>.Fail($"Failed to find records: {ex.Message}", ex);
        }
    }

    private object? GetDefaultValue(Type type)
    {
        if (type.IsValueType)
            return Activator.CreateInstance(type);
        return null;
    }

    private (string columns, string values, Dictionary<string, object?> parameters) BuildInsertParameters(T entity)
    {
        var columns = new List<string>();
        var values = new List<string>();
        var parameters = new Dictionary<string, object?>();

        foreach (var prop in GetCachedProperties())
        {
            var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr == null || columnAttr.AutoIncrement)
                continue;

            var columnName = columnAttr.Name ?? prop.Name;
            var value = prop.GetValue(entity);

            if (columnAttr.DefaultValue != null && value.Equals(GetDefaultValue(prop.PropertyType)))
                continue;


            columns.Add($"`{columnName}`");
            values.Add($"@{columnName}");

            var convertedValue = TypeConverter.ToMySql(value, columnAttr.DataType);
            parameters[$"@{columnName}"] = convertedValue;
        }

        return (string.Join(", ", columns), string.Join(", ", values), parameters);
    }

    private (string setClause, Dictionary<string, object?> parameters) BuildUpdateParameters(T entity)
    {
        var setClauses = new List<string>();
        var parameters = new Dictionary<string, object?>();

        foreach (var prop in GetCachedProperties())
        {
            var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
            var columnName = columnAttr.Name ?? prop.Name;

            if (columnAttr.Primary || columnAttr.AutoIncrement)
                continue;

            var value = prop.GetValue(entity);

            // If OnUpdateCurrentTimestamp is true, the value is already set in UpdateAsync, so include it.
            // Otherwise, if value is null and NotNull is false, skip it.
            if (value == null && !columnAttr.NotNull && !columnAttr.OnUpdateCurrentTimestamp)
                continue;

            setClauses.Add($"`{columnName}` = @{columnName}");

            var convertedValue = TypeConverter.ToMySql(value, columnAttr.DataType);
            parameters[$"@{columnName}"] = convertedValue;
        }

        return (string.Join(", ", setClauses), parameters);
    }

    private T MapReaderToEntity(MySqlDataReader reader)
    {
        var entity = new T();

        foreach (var prop in GetCachedProperties())
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

    private PropertyInfo? GetPrimaryKeyProperty()
    {
        return GetCachedProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Primary == true);
    }

    private void SetPrimaryKeyValue(T entity, object? value)
    {
        var primaryKey = GetPrimaryKeyProperty();
        if (primaryKey != null && primaryKey.GetCustomAttribute<ColumnAttribute>()?.AutoIncrement == true && value != null)
        {
            var convertedValue = Convert.ChangeType(value, primaryKey.PropertyType);
            primaryKey.SetValue(entity, convertedValue);
        }
    }

    private PropertyInfo[] GetCachedProperties()
    {
        var key = typeof(T).FullName ?? typeof(T).Name;
        if (!_propertyCache.TryGetValue(key, out var properties))
        {
            properties = typeof(T).GetProperties()
                .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null)
                .ToArray();
            _propertyCache[key] = properties;
        }
        return properties;
    }
}