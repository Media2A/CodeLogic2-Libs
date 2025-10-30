using CodeLogic.Abstractions;
using CL.PostgreSQL.Core;
using CL.PostgreSQL.Models;
using Npgsql;
using System.Reflection;

namespace CL.PostgreSQL.Services;

/// <summary>
/// Service for synchronizing model definitions with PostgreSQL database schema.
/// Handles automatic table creation, column management, and index synchronization.
/// </summary>
public class TableSyncService
{
    private readonly ConnectionManager _connectionManager;
    private readonly string _dataDirectory;
    private readonly ILogger? _logger;
    private readonly SchemaAnalyzer _schemaAnalyzer;
    private readonly BackupManager _backupManager;
    private readonly MigrationTracker _migrationTracker;

    public TableSyncService(ConnectionManager connectionManager, string dataDirectory, ILogger? logger)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _dataDirectory = dataDirectory;
        _logger = logger;
        _schemaAnalyzer = new SchemaAnalyzer(logger);
        _backupManager = new BackupManager(dataDirectory, logger);
        _migrationTracker = new MigrationTracker(dataDirectory, logger);
    }

    /// <summary>
    /// Synchronizes a single table with its model definition.
    /// </summary>
    public async Task<bool> SyncTableAsync<T>(
        string connectionId = "Default",
        bool createBackup = true) where T : class
    {
        try
        {
            _logger?.Info($"Starting table sync for {typeof(T).Name}");

            var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>();
            var tableName = tableAttr?.Name ?? typeof(T).Name;
            var schemaName = tableAttr?.Schema ?? "public";

            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                // Check if table exists
                var tableExists = await TableExistsAsync(connection, schemaName, tableName);

                if (!tableExists)
                {
                    // Create table
                    var createTableSql = _schemaAnalyzer.GenerateCreateTableSql<T>(schemaName);
                    using var cmd = new NpgsqlCommand(createTableSql, connection);
                    await cmd.ExecuteNonQueryAsync();

                    _logger?.Info($"Created table {schemaName}.{tableName}");
                    await _migrationTracker.RecordMigrationAsync(tableName, "CREATE TABLE");
                    return true;
                }

                // Table exists - sync schema
                if (createBackup)
                {
                    await _backupManager.CreateTableBackupAsync(connection, schemaName, tableName);
                }

                // Sync columns
                await SyncColumnsAsync<T>(connection, schemaName, tableName);

                // Sync indexes
                await SyncIndexesAsync<T>(connection, schemaName, tableName);

                _logger?.Info($"Synchronized table {schemaName}.{tableName}");
                await _migrationTracker.RecordMigrationAsync(tableName, "SYNC SCHEMA");
                return true;
            }, connectionId);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to sync table {typeof(T).Name}: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Synchronizes multiple tables at once.
    /// </summary>
    public async Task<Dictionary<string, bool>> SyncTablesAsync(
        Type[] modelTypes,
        string connectionId = "Default",
        bool createBackup = true)
    {
        var results = new Dictionary<string, bool>();

        foreach (var modelType in modelTypes)
        {
            try
            {
                var tableName = modelType.GetCustomAttribute<TableAttribute>()?.Name ?? modelType.Name;
                var method = GetType()
                    .GetMethod("SyncTableAsync", new[] { typeof(string), typeof(bool) })?
                    .MakeGenericMethod(modelType);

                if (method != null)
                {
                    var result = await (Task<bool>)method.Invoke(this, new object[] { connectionId, createBackup })!;
                    results[tableName] = result;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to sync table {modelType.Name}: {ex.Message}", ex);
                results[modelType.Name] = false;
            }
        }

        return results;
    }

    /// <summary>
    /// Synchronizes all tables in a specified namespace.
    /// </summary>
    public async Task<Dictionary<string, bool>> SyncNamespaceAsync(
        string namespaceName,
        string connectionId = "Default",
        bool createBackup = true,
        bool includeDerivedNamespaces = false)
    {
        try
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name != null);

            if (assembly == null)
                return new Dictionary<string, bool>();

            var types = assembly.GetTypes()
                .Where(t => includeDerivedNamespaces
                    ? t.Namespace?.StartsWith(namespaceName) == true
                    : t.Namespace == namespaceName)
                .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<TableAttribute>() != null)
                .ToArray();

            return await SyncTablesAsync(types, connectionId, createBackup);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to sync namespace {namespaceName}: {ex.Message}", ex);
            return new Dictionary<string, bool>();
        }
    }

    // Private Helper Methods

    private async Task<bool> TableExistsAsync(NpgsqlConnection connection, string schemaName, string tableName)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = @schema AND table_name = @table
            )";

        using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@schema", schemaName);
        cmd.Parameters.AddWithValue("@table", tableName);

        var result = await cmd.ExecuteScalarAsync();
        return result != null && Convert.ToBoolean(result);
    }

    private async Task SyncColumnsAsync<T>(NpgsqlConnection connection, string schemaName, string tableName)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null)
            .ToArray();

        // Get existing columns from database
        const string columnsSql = @"
            SELECT column_name FROM information_schema.columns
            WHERE table_schema = @schema AND table_name = @table";

        var existingColumns = new HashSet<string>();
        using (var cmd = new NpgsqlCommand(columnsSql, connection))
        {
            cmd.Parameters.AddWithValue("@schema", schemaName);
            cmd.Parameters.AddWithValue("@table", tableName);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                existingColumns.Add(reader.GetString(0));
            }
        }

        // Add missing columns
        foreach (var property in properties)
        {
            var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
            var columnName = columnAttr?.Name ?? property.Name;

            if (!existingColumns.Contains(columnName))
            {
                var columnDef = _schemaAnalyzer.GenerateColumnDefinition(property, columnAttr);
                var sql = $"ALTER TABLE \"{schemaName}\".\"{tableName}\" ADD COLUMN {columnDef}";

                using var cmd = new NpgsqlCommand(sql, connection);
                await cmd.ExecuteNonQueryAsync();

                _logger?.Info($"Added column {columnName} to {tableName}");
            }
        }

        // Remove columns no longer in model
        var modelColumnNames = properties
            .Select(p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name)
            .ToHashSet();

        foreach (var existingColumn in existingColumns)
        {
            if (!modelColumnNames.Contains(existingColumn))
            {
                var sql = $"ALTER TABLE \"{schemaName}\".\"{tableName}\" DROP COLUMN \"{existingColumn}\"";

                using var cmd = new NpgsqlCommand(sql, connection);
                await cmd.ExecuteNonQueryAsync();

                _logger?.Info($"Dropped column {existingColumn} from {tableName}");
            }
        }
    }

    private async Task SyncIndexesAsync<T>(NpgsqlConnection connection, string schemaName, string tableName)
    {
        var properties = typeof(T).GetProperties()
            .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null)
            .ToArray();

        // Get existing indexes
        const string indexesSql = @"
            SELECT indexname FROM pg_indexes
            WHERE schemaname = @schema AND tablename = @table
            AND indexname NOT LIKE '%_pkey%'";

        var existingIndexes = new HashSet<string>();
        using (var cmd = new NpgsqlCommand(indexesSql, connection))
        {
            cmd.Parameters.AddWithValue("@schema", schemaName);
            cmd.Parameters.AddWithValue("@table", tableName);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                existingIndexes.Add(reader.GetString(0));
            }
        }

        // Create missing indexes
        foreach (var property in properties)
        {
            var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr?.Index == true && !columnAttr.Primary)
            {
                var columnName = columnAttr.Name ?? property.Name;
                var indexName = $"idx_{tableName}_{columnName}";

                if (!existingIndexes.Contains(indexName))
                {
                    var isUnique = columnAttr.Unique ? "UNIQUE " : "";
                    var sql = $"CREATE {isUnique}INDEX \"{indexName}\" ON \"{schemaName}\".\"{tableName}\" (\"{columnName}\")";

                    using var cmd = new NpgsqlCommand(sql, connection);
                    await cmd.ExecuteNonQueryAsync();

                    _logger?.Info($"Created index {indexName}");
                }
            }
        }

        // Handle composite indexes
        var compositeIndexAttrs = typeof(T).GetCustomAttributes<CompositeIndexAttribute>();
        foreach (var indexAttr in compositeIndexAttrs)
        {
            if (!existingIndexes.Contains(indexAttr.IndexName))
            {
                var columns = string.Join("\", \"", indexAttr.ColumnNames);
                var isUnique = indexAttr.Unique ? "UNIQUE " : "";
                var sql = $"CREATE {isUnique}INDEX \"{indexAttr.IndexName}\" ON \"{schemaName}\".\"{tableName}\" (\"{columns}\")";

                using var cmd = new NpgsqlCommand(sql, connection);
                await cmd.ExecuteNonQueryAsync();

                _logger?.Info($"Created composite index {indexAttr.IndexName}");
            }
        }
    }
}

/// <summary>
/// Analyzes model definitions to generate PostgreSQL DDL statements.
/// </summary>
public class SchemaAnalyzer
{
    private readonly ILogger? _logger;

    public SchemaAnalyzer(ILogger? logger = null)
    {
        _logger = logger;
    }

    public string GenerateCreateTableSql<T>(string schemaName) where T : class
    {
        var type = typeof(T);
        var tableAttr = type.GetCustomAttribute<TableAttribute>();
        var tableName = tableAttr?.Name ?? type.Name;

        var columns = type.GetProperties()
            .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null)
            .Select(p => GenerateColumnDefinition(p, p.GetCustomAttribute<ColumnAttribute>()))
            .ToList();

        var primaryKeyProp = type.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<ColumnAttribute>()?.Primary == true);
        if (primaryKeyProp == null)
            primaryKeyProp = type.GetProperties().FirstOrDefault(p => p.Name == "Id");

        if (primaryKeyProp != null)
        {
            var pkColumnName = primaryKeyProp.GetCustomAttribute<ColumnAttribute>()?.Name ?? primaryKeyProp.Name;
            columns.Add($"PRIMARY KEY (\"{pkColumnName}\")");
        }

        var columnsSql = string.Join(",\n    ", columns);
        return $"CREATE TABLE IF NOT EXISTS \"{schemaName}\".\"{tableName}\" (\n    {columnsSql}\n)";
    }

    public string GenerateColumnDefinition(PropertyInfo property, ColumnAttribute? attr)
    {
        var columnName = attr?.Name ?? property.Name;
        var dataType = GetPostgreSQLDataType(attr?.DataType ?? DataType.VarChar, attr);
        var constraints = new List<string>();

        if (attr?.NotNull == true)
            constraints.Add("NOT NULL");

        if (attr?.Unique == true)
            constraints.Add("UNIQUE");

        if (attr?.AutoIncrement == true)
            constraints.Add("SERIAL");

        if (!string.IsNullOrEmpty(attr?.DefaultValue))
            constraints.Add($"DEFAULT {attr.DefaultValue}");

        if (attr?.OnUpdateCurrentTimestamp == true)
            constraints.Add("DEFAULT CURRENT_TIMESTAMP");

        var constraintsSql = constraints.Count > 0 ? " " + string.Join(" ", constraints) : "";
        return $"\"{columnName}\" {dataType}{constraintsSql}";
    }

    private string GetPostgreSQLDataType(DataType dataType, ColumnAttribute? attr)
    {
        return dataType switch
        {
            DataType.SmallInt => "SMALLINT",
            DataType.Int => "INTEGER",
            DataType.BigInt => "BIGINT",
            DataType.Real => "REAL",
            DataType.DoublePrecision => "DOUBLE PRECISION",
            DataType.Numeric => $"NUMERIC({attr?.Precision ?? 10},{attr?.Scale ?? 2})",
            DataType.Timestamp => "TIMESTAMP",
            DataType.TimestampTz => "TIMESTAMP WITH TIME ZONE",
            DataType.Date => "DATE",
            DataType.Time => "TIME",
            DataType.TimeTz => "TIME WITH TIME ZONE",
            DataType.Char => $"CHAR({attr?.Size ?? 255})",
            DataType.VarChar => $"VARCHAR({attr?.Size ?? 255})",
            DataType.Text => "TEXT",
            DataType.Json => "JSON",
            DataType.Jsonb => "JSONB",
            DataType.Uuid => "UUID",
            DataType.Bool => "BOOLEAN",
            DataType.Bytea => "BYTEA",
            DataType.IntArray => "INTEGER[]",
            DataType.BigIntArray => "BIGINT[]",
            DataType.TextArray => "TEXT[]",
            DataType.NumericArray => "NUMERIC[]",
            _ => "TEXT"
        };
    }
}

/// <summary>
/// Manages database backups before schema changes.
/// </summary>
public class BackupManager
{
    private readonly string _backupDirectory;
    private readonly ILogger? _logger;

    public BackupManager(string dataDirectory, ILogger? logger)
    {
        _backupDirectory = Path.Combine(dataDirectory, "backups");
        _logger = logger;
        Directory.CreateDirectory(_backupDirectory);
    }

    public async Task CreateTableBackupAsync(NpgsqlConnection connection, string schemaName, string tableName)
    {
        try
        {
            var backupTableName = $"{tableName}_backup_{DateTime.Now:yyyyMMdd_HHmmss}";
            var sql = $"CREATE TABLE \"{schemaName}\".\"{backupTableName}\" AS SELECT * FROM \"{schemaName}\".\"{tableName}\"";

            using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();

            _logger?.Info($"Created backup table {backupTableName}");
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Failed to create table backup: {ex.Message}");
        }
    }
}

/// <summary>
/// Tracks migration history for schema synchronization.
/// </summary>
public class MigrationTracker
{
    private readonly string _migrationFile;
    private readonly ILogger? _logger;

    public MigrationTracker(string dataDirectory, ILogger? logger)
    {
        _migrationFile = Path.Combine(dataDirectory, "migrations.log");
        _logger = logger;
    }

    public async Task RecordMigrationAsync(string tableName, string operation)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var entry = $"[{timestamp}] {operation} on {tableName}\n";
            await File.AppendAllTextAsync(_migrationFile, entry);
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Failed to record migration: {ex.Message}");
        }
    }
}
