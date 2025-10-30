using CodeLogic.Abstractions;
using CL.PostgreSQL.Core;
using CL.PostgreSQL.Models;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using System.Collections.Concurrent;
using System.Reflection;

namespace CL.PostgreSQL.Services;

/// <summary>
/// Generic repository for performing CRUD operations on model types.
/// Provides a high-level, type-safe interface for database operations.
/// </summary>
public class Repository<T> where T : class, new()
{
    private readonly string _connectionId;
    private readonly string _tableName;
    private readonly string _schemaName;
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
        _schemaName = tableAttr?.Schema ?? "public";

        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
    }

    /// <summary>
    /// Inserts a new record into the database.
    /// </summary>
    public async Task<OperationResult<T>> InsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var (columns, values, parameters) = BuildInsertParameters(entity);
                var sql = $"INSERT INTO \"{_schemaName}\".\"{_tableName}\" ({columns}) VALUES ({values}) RETURNING *";

                using var cmd = new NpgsqlCommand(sql, connection);
                foreach (var param in parameters)
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    entity = MapReaderToEntity(reader);
                    if (_config.EnableLogging)
                        _logger?.Debug($"Inserted record into {_schemaName}.{_tableName}");
                    return OperationResult<T>.Ok(entity, 1);
                }

                return OperationResult<T>.Ok(entity, 1);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to insert record into {_schemaName}.{_tableName}", ex);
            return OperationResult<T>.Fail($"Failed to insert record: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves a record by its primary key.
    /// </summary>
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
            _logger?.Error($"Failed to retrieve record from {_schemaName}.{_tableName}", ex);
            return OperationResult<T>.Fail($"Failed to retrieve record: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves a record by a specific column value.
    /// </summary>
    public async Task<OperationResult<T>> GetByColumnAsync(string columnName, object value, int cacheTtl = 0, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"{_tableName}:{columnName}:{value}";

            if (cacheTtl > 0 && _config.EnableCaching && _cache.TryGetValue<T>(cacheKey, out var cached))
                return OperationResult<T>.Ok(cached);

            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var sql = $"SELECT * FROM \"{_schemaName}\".\"{_tableName}\" WHERE \"{columnName}\" = @value LIMIT 1";

                using var cmd = new NpgsqlCommand(sql, connection);
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
            _logger?.Error($"Failed to retrieve record from {_schemaName}.{_tableName}", ex);
            return OperationResult<T>.Fail($"Failed to retrieve record: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves all records from the table.
    /// </summary>
    public async Task<OperationResult<List<T>>> GetAllAsync(int cacheTtl = 0, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"{_tableName}:all";

            if (cacheTtl > 0 && _config.EnableCaching && _cache.TryGetValue<List<T>>(cacheKey, out var cached))
                return OperationResult<List<T>>.Ok(cached);

            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var sql = $"SELECT * FROM \"{_schemaName}\".\"{_tableName}\"";
                using var cmd = new NpgsqlCommand(sql, connection);
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
            _logger?.Error($"Failed to retrieve records from {_schemaName}.{_tableName}", ex);
            return OperationResult<List<T>>.Fail($"Failed to retrieve records: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves a paginated set of records.
    /// </summary>
    public async Task<OperationResult<PagedResult<T>>> GetPagedAsync(int page, int pageSize, int cacheTtl = 0, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"{_tableName}:paged:{page}:{pageSize}";

            if (cacheTtl > 0 && _config.EnableCaching && _cache.TryGetValue<PagedResult<T>>(cacheKey, out var cached))
                return OperationResult<PagedResult<T>>.Ok(cached);

            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var totalItemsResult = await CountAsync(cancellationToken);
                var totalItems = totalItemsResult.Data;

                var offset = (page - 1) * pageSize;
                var sql = $"SELECT * FROM \"{_schemaName}\".\"{_tableName}\" LIMIT @pageSize OFFSET @offset";
                using var cmd = new NpgsqlCommand(sql, connection);
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
            _logger?.Error($"Failed to retrieve paged records from {_schemaName}.{_tableName}", ex);
            return OperationResult<PagedResult<T>>.Fail($"Failed to retrieve records: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Counts all records in the table.
    /// </summary>
    public async Task<OperationResult<long>> CountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var sql = $"SELECT COUNT(*) FROM \"{_schemaName}\".\"{_tableName}\"";
                using var cmd = new NpgsqlCommand(sql, connection);
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                var count = result != null ? Convert.ToInt64(result) : 0;
                return OperationResult<long>.Ok(count);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to count records in {_schemaName}.{_tableName}", ex);
            return OperationResult<long>.Fail($"Failed to count records: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates a record in the database.
    /// </summary>
    public async Task<OperationResult<T>> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var (setClause, parameters, primaryKeyValue) = BuildUpdateParameters(entity);
                var sql = $"UPDATE \"{_schemaName}\".\"{_tableName}\" SET {setClause} WHERE \"id\" = @id RETURNING *";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", primaryKeyValue ?? DBNull.Value);
                foreach (var param in parameters)
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    entity = MapReaderToEntity(reader);
                    if (_config.EnableLogging)
                        _logger?.Debug($"Updated record in {_schemaName}.{_tableName}");
                    return OperationResult<T>.Ok(entity, 1);
                }

                return OperationResult<T>.Ok(entity, 1);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to update record in {_schemaName}.{_tableName}", ex);
            return OperationResult<T>.Fail($"Failed to update record: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes a record by its primary key.
    /// </summary>
    public async Task<OperationResult<bool>> DeleteAsync(object id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                var sql = $"DELETE FROM \"{_schemaName}\".\"{_tableName}\" WHERE \"id\" = @id";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@id", id ?? DBNull.Value);

                var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);

                if (_config.EnableLogging)
                    _logger?.Debug($"Deleted record from {_schemaName}.{_tableName}");

                return OperationResult<bool>.Ok(rowsAffected > 0, rowsAffected);
            }, _connectionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to delete record from {_schemaName}.{_tableName}", ex);
            return OperationResult<bool>.Fail($"Failed to delete record: {ex.Message}", ex);
        }
    }

    // Helper Methods

    private PropertyInfo? GetPrimaryKeyProperty()
    {
        var properties = GetProperties();
        return properties.FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Primary == true) ??
               properties.FirstOrDefault(p => p.Name == "Id");
    }

    private PropertyInfo[] GetProperties()
    {
        var key = typeof(T).FullName ?? typeof(T).Name;
        return _propertyCache.GetOrAdd(key, _ =>
            typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null)
                .ToArray()
        );
    }

    private (string columns, string values, Dictionary<string, object?> parameters) BuildInsertParameters(T entity)
    {
        var columns = new List<string>();
        var values = new List<string>();
        var parameters = new Dictionary<string, object?>();
        var paramIndex = 0;

        foreach (var property in GetProperties())
        {
            var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr?.AutoIncrement == true)
                continue;

            var columnName = columnAttr?.Name ?? property.Name;
            var value = property.GetValue(entity);

            columns.Add($"\"{columnName}\"");
            var paramName = $"@p{paramIndex}";
            values.Add(paramName);
            parameters[paramName] = value;

            paramIndex++;
        }

        return (string.Join(", ", columns), string.Join(", ", values), parameters);
    }

    private (string setClause, Dictionary<string, object?> parameters, object? primaryKeyValue) BuildUpdateParameters(T entity)
    {
        var setClauses = new List<string>();
        var parameters = new Dictionary<string, object?>();
        var primaryKeyValue = GetPrimaryKeyValue(entity);
        var paramIndex = 0;

        foreach (var property in GetProperties())
        {
            var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr?.Primary == true || columnAttr?.AutoIncrement == true)
                continue;

            var columnName = columnAttr?.Name ?? property.Name;
            var value = property.GetValue(entity);

            var paramName = $"@p{paramIndex}";
            setClauses.Add($"\"{columnName}\" = {paramName}");
            parameters[paramName] = value;

            paramIndex++;
        }

        return (string.Join(", ", setClauses), parameters, primaryKeyValue);
    }

    private object? GetPrimaryKeyValue(T entity)
    {
        var primaryKey = GetPrimaryKeyProperty();
        return primaryKey?.GetValue(entity);
    }

    private void SetPrimaryKeyValue(T entity, object? value)
    {
        if (value == null || value == DBNull.Value)
            return;

        var primaryKey = GetPrimaryKeyProperty();
        if (primaryKey != null)
            primaryKey.SetValue(entity, Convert.ChangeType(value, primaryKey.PropertyType));
    }

    private T MapReaderToEntity(NpgsqlDataReader reader)
    {
        var entity = new T();
        var properties = GetProperties();

        foreach (var property in properties)
        {
            var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
            var columnName = columnAttr?.Name ?? property.Name;

            try
            {
                if (reader.IsDBNull(reader.GetOrdinal(columnName)))
                {
                    property.SetValue(entity, null);
                    continue;
                }

                var value = reader.GetValue(reader.GetOrdinal(columnName));
                var dataType = columnAttr?.DataType ?? DataType.VarChar;
                var convertedValue = TypeConverter.FromPostgreSQL(value, dataType, property.PropertyType);
                property.SetValue(entity, convertedValue);
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Failed to map column {columnName} to property {property.Name}: {ex.Message}");
            }
        }

        return entity;
    }
}
