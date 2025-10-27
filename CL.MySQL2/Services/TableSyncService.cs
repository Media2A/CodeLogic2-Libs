using CodeLogic.Abstractions;
using CL.MySQL2.Models;
using MySqlConnector;
using System.Reflection;
using System.Text;

namespace CL.MySQL2.Services;

/// <summary>
/// Synchronizes C# model definitions with MySQL database tables.
/// Handles table creation, schema synchronization, and migration tracking.
/// </summary>
public class TableSyncService
{
    private readonly ConnectionManager _connectionManager;
    private readonly SchemaAnalyzer _schemaAnalyzer;
    private readonly BackupManager _backupManager;
    private readonly MigrationTracker _migrationTracker;
    private readonly ILogger? _logger;
    private const string LogFileName = "TableSync";

    public TableSyncService(
        ConnectionManager connectionManager,
        string dataDirectory,
        ILogger? logger = null)
    {
        _connectionManager = connectionManager;
        _logger = logger;
        _schemaAnalyzer = new SchemaAnalyzer(logger);
        _backupManager = new BackupManager(dataDirectory, logger);
        _migrationTracker = new MigrationTracker(dataDirectory, logger);
    }

    /// <summary>
    /// Synchronizes a single model type with its database table.
    /// </summary>
    public async Task<bool> SyncTableAsync<T>(
        string connectionId = "Default",
        bool createBackup = true) where T : class
    {
        try
        {
            var modelType = typeof(T);
            var tableName = GetTableName(modelType);

            _logger?.Info($"Starting table synchronization for model '{modelType.Name}' (table: '{tableName}')");

            return await SyncTableInternalAsync(modelType, tableName, connectionId, createBackup);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to sync table for model '{typeof(T).Name}': {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Synchronizes multiple model types at once.
    /// </summary>
    public async Task<Dictionary<string, bool>> SyncTablesAsync(
        Type[] modelTypes,
        string connectionId = "Default",
        bool createBackup = true)
    {
        var results = new Dictionary<string, bool>();

        _logger?.Info($"Starting batch table synchronization for {modelTypes.Length} model(s)");

        foreach (var modelType in modelTypes)
        {
            var tableName = GetTableName(modelType);

            try
            {
                var result = await SyncTableInternalAsync(modelType, tableName, connectionId, createBackup);
                results[tableName] = result;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to sync table '{tableName}': {ex.Message}", ex);
                results[tableName] = false;
            }
        }

        _logger?.Info($"Batch synchronization completed. Success: {results.Count(r => r.Value)}/{results.Count}");
        return results;
    }

    /// <summary>
    /// Internal method that performs the actual table synchronization.
    /// </summary>
    private async Task<bool> SyncTableInternalAsync(
        Type modelType,
        string tableName,
        string connectionId,
        bool createBackup)
    {
        try
        {
            return await _connectionManager.ExecuteWithConnectionAsync(async connection =>
            {
                // Get table attributes
                var tableAttr = modelType.GetCustomAttribute<TableAttribute>();
                var actualTableName = tableAttr?.Name ?? tableName;

                _logger?.Info($"--- Syncing table: '{actualTableName}' ---");

                // Generate model columns
                var modelColumns = _schemaAnalyzer.GenerateModelColumnDefinitions(modelType);

                if (!modelColumns.Any())
                {
                    _logger?.Debug($"No columns found for '{actualTableName}'. Skipping table creation.");
                    return false;
                }

                // Check if table exists
                var tableExists = await _schemaAnalyzer.TableExistsAsync(connection, actualTableName);

                if (!tableExists)
                {
                    // Table doesn't exist - create it
                    _logger?.Info($"Table '{actualTableName}' does not exist. Creating it now.");

                    var createTableSql = _schemaAnalyzer.GenerateCreateTableStatement(
                        actualTableName,
                        modelColumns,
                        tableAttr);

                    using var cmd = new MySqlCommand(createTableSql, connection);
                    await cmd.ExecuteNonQueryAsync();

                    _logger?.Info($"Successfully created table '{actualTableName}'");

                    // Create indexes
                    await CreateIndexesAsync(connection, actualTableName, modelColumns);

                    // Record migration
                    await _migrationTracker.RecordMigrationAsync(
                        actualTableName,
                        "CREATE",
                        connectionId,
                        $"Table created from model '{modelType.Name}'");

                    return true;
                }
                else
                {
                    // Table exists - sync schema
                    _logger?.Info($"Table '{actualTableName}' exists. Syncing columns, keys, and indexes.");

                    // Create backup before making changes
                    if (createBackup)
                    {
                        await _backupManager.BackupTableSchemaAsync(connection, actualTableName, connectionId);
                    }

                    // Get existing database columns
                    var dbColumns = await _schemaAnalyzer.GetDatabaseColumnsAsync(connection, actualTableName);

                    // Sync columns
                    await SyncColumnsAsync(
                        connection,
                        actualTableName,
                        modelColumns,
                        dbColumns);

                    // Sync primary key
                    var primaryKey = modelColumns.FirstOrDefault(c => c.Primary);
                    if (primaryKey != null)
                    {
                        await SyncPrimaryKeyAsync(connection, actualTableName, primaryKey.ColumnName);
                    }

                    // Sync table engine if needed
                    if (tableAttr != null)
                    {
                        await SyncTableEngineAsync(connection, actualTableName, tableAttr.Engine);
                    }

                    // Sync indexes
                    await SyncIndexesAsync(connection, actualTableName, modelColumns);

                    // Record migration
                    await _migrationTracker.RecordMigrationAsync(
                        actualTableName,
                        "ALTER",
                        connectionId,
                        $"Table schema synchronized from model '{modelType.Name}'");

                    return true;
                }

            }, connectionId);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Table sync failed for '{tableName}': {ex.Message}", ex);

            // Record failed migration
            await _migrationTracker.RecordMigrationAsync(
                tableName,
                "SYNC",
                connectionId,
                success: false,
                errorMessage: ex.Message);

            return false;
        }
    }

    /// <summary>
    /// Synchronizes columns between model and database.
    /// </summary>
    private async Task SyncColumnsAsync(
        MySqlConnection connection,
        string tableName,
        List<SchemaAnalyzer.ModelColumnDefinition> modelColumns,
        List<SchemaAnalyzer.DatabaseColumnDefinition> dbColumns)
    {
        var alterationsSummary = new StringBuilder();

        // Check for columns to drop
        foreach (var dbColumn in dbColumns)
        {
            var matchedColumn = modelColumns.FirstOrDefault(
                c => c.ColumnName.Equals(dbColumn.ColumnName, StringComparison.OrdinalIgnoreCase));

            if (matchedColumn == null)
            {
                alterationsSummary.AppendLine($"  - Dropping column: `{dbColumn.ColumnName}`");
                var alterSql = $"ALTER TABLE `{tableName}` DROP COLUMN `{dbColumn.ColumnName}`";

                _logger?.Debug($"Executing: {alterSql}");

                using var cmd = new MySqlCommand(alterSql, connection);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // Check for columns to add or modify
        foreach (var modelColumn in modelColumns)
        {
            var dbColumn = dbColumns.FirstOrDefault(
                c => c.ColumnName.Equals(modelColumn.ColumnName, StringComparison.OrdinalIgnoreCase));

            if (dbColumn == null)
            {
                // Add new column
                alterationsSummary.AppendLine($"  - Adding column: `{modelColumn.ColumnName}`");

                var columnDef = _schemaAnalyzer.GenerateCreateTableStatement(
                    "temp",
                    new List<SchemaAnalyzer.ModelColumnDefinition> { modelColumn });

                // Extract just the column definition
                var columnDefStart = columnDef.IndexOf('(') + 1;
                var columnDefEnd = columnDef.LastIndexOf(')');
                var columnDefContent = columnDef.Substring(columnDefStart, columnDefEnd - columnDefStart).Trim();
                var columnDefParts = columnDefContent.Split(',').First().Trim();

                var alterSql = $"ALTER TABLE `{tableName}` ADD COLUMN {columnDefParts}";

                _logger?.Debug($"Executing: {alterSql}");

                using var cmd = new MySqlCommand(alterSql, connection);
                await cmd.ExecuteNonQueryAsync();
            }
            else if (ColumnDefinitionChanged(modelColumn, dbColumn))
            {
                // Modify existing column
                alterationsSummary.AppendLine($"  - Modifying column: `{modelColumn.ColumnName}`");

                var columnDef = _schemaAnalyzer.GenerateCreateTableStatement(
                    "temp",
                    new List<SchemaAnalyzer.ModelColumnDefinition> { modelColumn });

                var columnDefStart = columnDef.IndexOf('(') + 1;
                var columnDefEnd = columnDef.LastIndexOf(')');
                var columnDefContent = columnDef.Substring(columnDefStart, columnDefEnd - columnDefStart).Trim();
                var columnDefParts = columnDefContent.Split(',').First().Trim();

                var alterSql = $"ALTER TABLE `{tableName}` MODIFY COLUMN {columnDefParts}";

                _logger?.Debug($"Executing: {alterSql}");

                using var cmd = new MySqlCommand(alterSql, connection);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        if (alterationsSummary.Length > 0)
        {
            _logger?.Info($"Column changes applied to table '{tableName}':\n{alterationsSummary}");
        }
        else
        {
            _logger?.Info($"No column changes required for table '{tableName}'.");
        }
    }

    /// <summary>
    /// Checks if a column definition has changed.
    /// </summary>
    private bool ColumnDefinitionChanged(
        SchemaAnalyzer.ModelColumnDefinition modelCol,
        SchemaAnalyzer.DatabaseColumnDefinition dbCol)
    {
        // Simple comparison - can be enhanced for more detailed checks
        var typeMatch = dbCol.DataType.Equals(ConvertDataTypeToMysql(modelCol.DataType), StringComparison.OrdinalIgnoreCase);
        var nullMatch = dbCol.Nullable == !modelCol.NotNull;

        return !typeMatch || !nullMatch;
    }

    /// <summary>
    /// Synchronizes the primary key.
    /// </summary>
    private async Task SyncPrimaryKeyAsync(MySqlConnection connection, string tableName, string primaryKeyColumn)
    {
        try
        {
            // Check current primary key
            using var cmd = new MySqlCommand(
                $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName AND CONSTRAINT_NAME = 'PRIMARY'",
                connection);
            cmd.Parameters.AddWithValue("@tableName", tableName);

            using var reader = await cmd.ExecuteReaderAsync();
            string? existingPrimaryKey = null;

            if (await reader.ReadAsync())
            {
                existingPrimaryKey = reader.GetString(0);
            }

            if (string.IsNullOrEmpty(existingPrimaryKey) && !string.IsNullOrEmpty(primaryKeyColumn))
            {
                _logger?.Info($"Adding PRIMARY KEY on '{primaryKeyColumn}' to table '{tableName}'.");
                var addPkSql = $"ALTER TABLE `{tableName}` ADD PRIMARY KEY (`{primaryKeyColumn}`)";
                using var addCmd = new MySqlCommand(addPkSql, connection);
                await addCmd.ExecuteNonQueryAsync();
            }
            else if (!string.IsNullOrEmpty(existingPrimaryKey) && existingPrimaryKey != primaryKeyColumn)
            {
                _logger?.Info($"Changing PRIMARY KEY for '{tableName}' from '{existingPrimaryKey}' to '{primaryKeyColumn}'.");
                var dropPkSql = $"ALTER TABLE `{tableName}` DROP PRIMARY KEY";
                var addPkSql = $"ALTER TABLE `{tableName}` ADD PRIMARY KEY (`{primaryKeyColumn}`)";

                using var dropCmd = new MySqlCommand(dropPkSql, connection);
                await dropCmd.ExecuteNonQueryAsync();

                using var addCmd = new MySqlCommand(addPkSql, connection);
                await addCmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Failed to sync primary key for '{tableName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Synchronizes the table engine if necessary.
    /// </summary>
    private async Task SyncTableEngineAsync(MySqlConnection connection, string tableName, TableEngine targetEngine)
    {
        try
        {
            using var cmd = new MySqlCommand(
                "SELECT ENGINE FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName",
                connection);
            cmd.Parameters.AddWithValue("@tableName", tableName);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var currentEngine = reader.GetString(0);

                if (!currentEngine.Equals(targetEngine.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.Info($"Updating storage engine for '{tableName}' from {currentEngine} to {targetEngine}.");
                    var alterEngineSql = $"ALTER TABLE `{tableName}` ENGINE={targetEngine}";

                    using var alterCmd = new MySqlCommand(alterEngineSql, connection);
                    await alterCmd.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Failed to sync table engine for '{tableName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Creates indexes for a new table.
    /// </summary>
    private async Task CreateIndexesAsync(
        MySqlConnection connection,
        string tableName,
        List<SchemaAnalyzer.ModelColumnDefinition> columns)
    {
        _logger?.Info($"Creating indexes for new table '{tableName}'.");

        foreach (var col in columns)
        {
            if (col.Index && col.Unique)
            {
                var indexName = $"idx_uniq_{col.ColumnName}_{tableName}";
                var sql = $"CREATE UNIQUE INDEX `{indexName}` ON `{tableName}` (`{col.ColumnName}`)";

                _logger?.Debug($"Executing: {sql}");

                using var cmd = new MySqlCommand(sql, connection);
                await cmd.ExecuteNonQueryAsync();
            }
            else if (col.Index)
            {
                var indexName = $"idx_{col.ColumnName}_{tableName}";
                var sql = $"CREATE INDEX `{indexName}` ON `{tableName}` (`{col.ColumnName}`)";

                _logger?.Debug($"Executing: {sql}");

                using var cmd = new MySqlCommand(sql, connection);
                await cmd.ExecuteNonQueryAsync();
            }
            else if (col.Unique && !col.Primary)
            {
                var sql = $"ALTER TABLE `{tableName}` ADD UNIQUE (`{col.ColumnName}`)";

                _logger?.Debug($"Executing: {sql}");

                using var cmd = new MySqlCommand(sql, connection);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }

    /// <summary>
    /// Synchronizes indexes for existing tables.
    /// </summary>
    private async Task SyncIndexesAsync(
        MySqlConnection connection,
        string tableName,
        List<SchemaAnalyzer.ModelColumnDefinition> columns)
    {
        var existingIndexes = await _schemaAnalyzer.GetTableIndexesAsync(connection, tableName);

        foreach (var col in columns)
        {
            string indexName = $"idx_{col.ColumnName}_{tableName}";
            string uniqueIndexName = $"idx_uniq_{col.ColumnName}_{tableName}";

            var hasIndex = existingIndexes.Any(i => i.IndexName == indexName && !i.IsUnique);
            var hasUniqueIndex = existingIndexes.Any(i => i.IndexName == uniqueIndexName && i.IsUnique);

            if (col.Index && col.Unique)
            {
                if (!hasUniqueIndex)
                {
                    _logger?.Info($"Creating UNIQUE INDEX on {tableName}.{col.ColumnName}");
                    var sql = $"CREATE UNIQUE INDEX `{uniqueIndexName}` ON `{tableName}` (`{col.ColumnName}`)";

                    using var cmd = new MySqlCommand(sql, connection);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            else if (col.Index)
            {
                if (!hasIndex)
                {
                    _logger?.Info($"Creating INDEX on {tableName}.{col.ColumnName}");
                    var sql = $"CREATE INDEX `{indexName}` ON `{tableName}` (`{col.ColumnName}`)";

                    using var cmd = new MySqlCommand(sql, connection);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            else if (col.Unique && !col.Primary)
            {
                var hasUniqueConstraint = existingIndexes.Any(i =>
                    i.Columns.Count == 1 && i.Columns[0] == col.ColumnName && i.IsUnique && i.IndexName != "PRIMARY");

                if (!hasUniqueConstraint)
                {
                    _logger?.Info($"Creating UNIQUE CONSTRAINT on {tableName}.{col.ColumnName}");
                    var sql = $"ALTER TABLE `{tableName}` ADD UNIQUE (`{col.ColumnName}`)";

                    using var cmd = new MySqlCommand(sql, connection);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }

    /// <summary>
    /// Gets the table name from a model type.
    /// </summary>
    private string GetTableName(Type modelType)
    {
        var tableAttr = modelType.GetCustomAttribute<TableAttribute>();
        return tableAttr?.Name ?? modelType.Name;
    }

    /// <summary>
    /// Converts DataType enum to MySQL data type string.
    /// </summary>
    private string ConvertDataTypeToMysql(DataType dataType)
    {
        return dataType switch
        {
            DataType.TinyInt => "TINYINT",
            DataType.SmallInt => "SMALLINT",
            DataType.MediumInt => "MEDIUMINT",
            DataType.Int => "INT",
            DataType.BigInt => "BIGINT",
            DataType.Float => "FLOAT",
            DataType.Double => "DOUBLE",
            DataType.Decimal => "DECIMAL",
            DataType.DateTime => "DATETIME",
            DataType.Date => "DATE",
            DataType.Time => "TIME",
            DataType.Timestamp => "TIMESTAMP",
            DataType.Year => "YEAR",
            DataType.Char => "CHAR",
            DataType.VarChar => "VARCHAR",
            DataType.TinyText => "TINYTEXT",
            DataType.Text => "TEXT",
            DataType.MediumText => "MEDIUMTEXT",
            DataType.LongText => "LONGTEXT",
            DataType.Json => "JSON",
            DataType.Binary => "BINARY",
            DataType.VarBinary => "VARBINARY",
            DataType.TinyBlob => "TINYBLOB",
            DataType.Blob => "BLOB",
            DataType.MediumBlob => "MEDIUMBLOB",
            DataType.LongBlob => "LONGBLOB",
            DataType.Uuid => "CHAR",
            DataType.Enum => "ENUM",
            DataType.Set => "SET",
            DataType.Bool => "TINYINT",
            _ => "VARCHAR"
        };
    }

    /// <summary>
    /// Gets the migration tracker instance for accessing migration history.
    /// </summary>
    public MigrationTracker GetMigrationTracker() => _migrationTracker;

    /// <summary>
    /// Gets the backup manager instance for accessing backups.
    /// </summary>
    public BackupManager GetBackupManager() => _backupManager;
}
