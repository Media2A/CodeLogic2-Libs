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

    /// <summary>
    /// Initializes a new instance of the <see cref="TableSyncService"/> class.
    /// </summary>
    /// <param name="connectionManager">The connection manager for database access.</param>
    /// <param name="dataDirectory">The directory for storing backups and migration data.</param>
    /// <param name="logger">The logger for recording operations and errors.</param>
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
    /// Synchronizes all model types in a specified namespace.
    /// </summary>
    public async Task<Dictionary<string, bool>> SyncNamespaceAsync(
        string namespaceName,
        string connectionId = "Default",
        bool createBackup = true,
        bool includeDerivedNamespaces = false)
    {
        try
        {
            _logger?.Info($"Starting namespace synchronization for '{namespaceName}' (includeDerived: {includeDerivedNamespaces})");

            // Get all types in the specified namespace
            var modelTypes = GetTypesInNamespace(namespaceName, includeDerivedNamespaces)
                .Where(t =>
                    !t.IsAbstract &&
                    !t.IsInterface &&
                    !t.IsGenericTypeDefinition &&
                    t.GetCustomAttribute<TableAttribute>() != null)
                .ToArray();

            if (!modelTypes.Any())
            {
                _logger?.Warning($"No model classes with [Table] attribute found in namespace '{namespaceName}'");
                return new Dictionary<string, bool>();
            }

            _logger?.Info($"Found {modelTypes.Length} model(s) in namespace '{namespaceName}'. Starting synchronization...");

            return await SyncTablesAsync(modelTypes, connectionId, createBackup);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to sync namespace '{namespaceName}': {ex.Message}", ex);
            return new Dictionary<string, bool>();
        }
    }

    /// <summary>
    /// Gets all types in a specified namespace from all loaded assemblies.
    /// </summary>
    private IEnumerable<Type> GetTypesInNamespace(string namespaceName, bool includeDerivedNamespaces = false)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var query = new List<Type>();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(t => t.Namespace != null);

                if (includeDerivedNamespaces)
                {
                    types = types.Where(t => t.Namespace.StartsWith(namespaceName));
                }
                else
                {
                    types = types.Where(t => t.Namespace == namespaceName);
                }

                query.AddRange(types);
            }
            catch (Exception ex)
            {
                _logger?.Debug($"Could not load types from assembly '{assembly.FullName}': {ex.Message}");
            }
        }

        return query;
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
                var config = _connectionManager.GetConfiguration(connectionId);
                var syncMode = config.SyncMode;

                if (syncMode == SyncMode.None)
                {
                    _logger?.Info($"Synchronization is disabled for connection '{connectionId}'. Skipping.");
                    return true;
                }

                // Get table attributes
                var tableAttr = modelType.GetCustomAttribute<TableAttribute>();
                var actualTableName = tableAttr?.Name ?? tableName;

                _logger?.Info($"--- Syncing table: '{actualTableName}' with mode: {syncMode} ---");

                // Generate model columns
                var modelColumns = _schemaAnalyzer.GenerateModelColumnDefinitions(modelType);

                if (!modelColumns.Any())
                {
                    _logger?.Debug($"No columns found for '{actualTableName}'. Skipping table creation.");
                    return false;
                }

                // Check if table exists
                var tableExists = await _schemaAnalyzer.TableExistsAsync(connection, actualTableName);

                // Handle Destructive mode
                if (syncMode == SyncMode.Destructive && tableExists)
                {
                    _logger?.Warning($"Destructive sync mode enabled. Dropping table '{actualTableName}'...");
                    using var dropCmd = new MySqlCommand($"DROP TABLE `{actualTableName}`", connection);
                    await dropCmd.ExecuteNonQueryAsync();
                    tableExists = false;
                    _logger?.Info($"Table '{actualTableName}' dropped successfully.");
                }

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
                    await CreateIndexesAsync(connection, actualTableName, modelType, modelColumns);

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
                        dbColumns,
                        syncMode);

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
                    await SyncIndexesAsync(connection, actualTableName, modelType, modelColumns);

                    // Sync Foreign Keys
                    await SyncForeignKeysAsync(connection, actualTableName, modelType);

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
        List<SchemaAnalyzer.DatabaseColumnDefinition> dbColumns,
        SyncMode syncMode)
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

                try
                {
                    using var cmd = new MySqlCommand(alterSql, connection);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (MySqlException ex) when (syncMode == SyncMode.Reconstruct && (ex.Number == 1822 || ex.Number == 1217 || ex.Number == 1451))
                {
                    _logger?.Warning($"Could not modify column '{modelColumn.ColumnName}' directly due to a foreign key constraint. Attempting to reconstruct constraints. Error: {ex.Message}");
                    await HandleReconstruction(connection, tableName, alterSql);
                }
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

    private async Task HandleReconstruction(MySqlConnection connection, string tableName, string originalAlterSql)
    {
        // 1. Get all related foreign keys
        var relatedFks = await GetRelatedForeignKeysAsync(connection, tableName);
        if (!relatedFks.Any())
        {
            _logger?.Error("Reconstruction failed: Could not find any foreign keys to drop.");
            throw new InvalidOperationException("Reconstruction failed despite foreign key error.");
        }

        _logger?.Info($"Found {relatedFks.Count} foreign key(s) to reconstruct for table '{tableName}'.");

        // 2. Drop them
        await DropForeignKeysAsync(connection, relatedFks);

        // 3. Retry the original command
        _logger?.Info("Retrying original ALTER TABLE command...");
        using (var retryCmd = new MySqlCommand(originalAlterSql, connection))
        {
            await retryCmd.ExecuteNonQueryAsync();
        }
        _logger?.Info("Original command succeeded after dropping constraints.");

        // 4. Recreate them
        await RecreateForeignKeysAsync(connection, relatedFks);
    }

    private async Task<List<ForeignKeyInfo>> GetRelatedForeignKeysAsync(MySqlConnection connection, string tableName)
    {
        var fks = new List<ForeignKeyInfo>();
        var sql = @"
            SELECT 
                kcu.constraint_name, 
                kcu.table_name, 
                kcu.column_name, 
                kcu.referenced_table_name, 
                kcu.referenced_column_name,
                rc.update_rule,
                rc.delete_rule
            FROM information_schema.key_column_usage AS kcu
            JOIN information_schema.referential_constraints AS rc ON kcu.constraint_name = rc.constraint_name AND kcu.table_schema = rc.constraint_schema
            WHERE kcu.table_schema = DATABASE() AND (kcu.table_name = @tableName OR kcu.referenced_table_name = @tableName);
        ";
        
        using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@tableName", tableName);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            fks.Add(new ForeignKeyInfo
            {
                ConstraintName = reader.GetString("constraint_name"),
                TableName = reader.GetString("table_name"),
                ColumnName = reader.GetString("column_name"),
                ReferencedTableName = reader.GetString("referenced_table_name"),
                ReferencedColumnName = reader.GetString("referenced_column_name"),
                OnUpdate = reader.GetString("update_rule"),
                OnDelete = reader.GetString("delete_rule")
            });
        }
        return fks;
    }

    private async Task DropForeignKeysAsync(MySqlConnection connection, List<ForeignKeyInfo> fks)
    {
        foreach (var fk in fks)
        {
            _logger?.Info($"Dropping foreign key '{fk.ConstraintName}' on table '{fk.TableName}'.");
            var dropSql = $"ALTER TABLE `{fk.TableName}` DROP FOREIGN KEY `{fk.ConstraintName}`;";
            using var cmd = new MySqlCommand(dropSql, connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private async Task RecreateForeignKeysAsync(MySqlConnection connection, List<ForeignKeyInfo> fks)
    {
        foreach (var fk in fks)
        {
            _logger?.Info($"Recreating foreign key '{fk.ConstraintName}' on table '{fk.TableName}'.");
            var addSql = $"ALTER TABLE `{fk.TableName}` ADD CONSTRAINT `{fk.ConstraintName}` FOREIGN KEY (`{fk.ColumnName}`) REFERENCES `{fk.ReferencedTableName}` (`{fk.ReferencedColumnName}`) ON DELETE {fk.OnDelete} ON UPDATE {fk.OnUpdate};";
            using var cmd = new MySqlCommand(addSql, connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private class ForeignKeyInfo
    {
        public string ConstraintName { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string ReferencedTableName { get; set; }
        public string ReferencedColumnName { get; set; }
        public string OnUpdate { get; set; }
        public string OnDelete { get; set; }
    }

    /// <summary>
    /// Checks if a column definition has changed between the model and the database.
    /// </summary>
    private bool ColumnDefinitionChanged(
        SchemaAnalyzer.ModelColumnDefinition modelCol,
        SchemaAnalyzer.DatabaseColumnDefinition dbCol)
    {
        // 1. Compare Data Type and related properties (Size, Precision, Scale, Unsigned)
        // Generate the expected full column type string from the model
        var expectedModelColumnType = _schemaAnalyzer.GenerateColumnDefinition(modelCol)
                                                    .Replace($"`{modelCol.ColumnName}` ", "") // Remove column name
                                                    .Trim();

        // The dbCol.ColumnType already contains size/precision/unsigned etc.
        // Example: VARCHAR(255), INT UNSIGNED, DECIMAL(10,2)
        if (!dbCol.ColumnType.Equals(expectedModelColumnType, StringComparison.OrdinalIgnoreCase))
        {
            _logger?.Debug($"Column '{modelCol.ColumnName}' type mismatch. Model: '{expectedModelColumnType}', DB: '{dbCol.ColumnType}'");
            return true;
        }

        // 2. Compare Nullability
        // dbCol.Nullable is true if it CAN be null, modelCol.NotNull is true if it CANNOT be null
        if (dbCol.Nullable == modelCol.NotNull)
        {
            _logger?.Debug($"Column '{modelCol.ColumnName}' nullability mismatch. Model NotNull: '{modelCol.NotNull}', DB Nullable: '{dbCol.Nullable}'");
            return true;
        }

        // 3. Compare AutoIncrement
        if (dbCol.AutoIncrement != modelCol.AutoIncrement)
        {
            _logger?.Debug($"Column '{modelCol.ColumnName}' auto-increment mismatch. Model: '{modelCol.AutoIncrement}', DB: '{dbCol.AutoIncrement}'");
            return true;
        }

        // 4. Compare Default Value
        // Normalize default values for comparison (e.g., NULL vs null, CURRENT_TIMESTAMP vs current_timestamp())
        var normalizedModelDefault = modelCol.DefaultValue?.ToUpper() ?? (modelCol.NotNull ? "" : "NULL");
        var normalizedDbDefault = dbCol.DefaultValue?.ToUpper() ?? (dbCol.Nullable ? "NULL" : "");

        // MySQL sometimes returns 'NULL' for actual NULL default, sometimes null. Handle CURRENT_TIMESTAMP variations.
        if (normalizedModelDefault == "CURRENT_TIMESTAMP" && normalizedDbDefault.Contains("CURRENT_TIMESTAMP"))
        {
            // Match, do nothing
        }
        else if (!normalizedModelDefault.Equals(normalizedDbDefault, StringComparison.OrdinalIgnoreCase))
        {
            _logger?.Debug($"Column '{modelCol.ColumnName}' default value mismatch. Model: '{normalizedModelDefault}', DB: '{normalizedDbDefault}'");
            return true;
        }

        // 5. Compare OnUpdateCurrentTimestamp
        var modelOnUpdate = modelCol.OnUpdateCurrentTimestamp;
        var dbOnUpdate = dbCol.Extra?.Contains("on update CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase) ?? false;
        if (modelOnUpdate != dbOnUpdate)
        {
            _logger?.Debug($"Column '{modelCol.ColumnName}' on update timestamp mismatch. Model: '{modelOnUpdate}', DB: '{dbOnUpdate}'");
            return true;
        }

        // 6. Compare Charset (if specified in model and applicable to type)
        if (modelCol.Charset.HasValue && (modelCol.DataType == DataType.VarChar || modelCol.DataType == DataType.Char || modelCol.DataType == DataType.Text))
        {
            var expectedCharset = _schemaAnalyzer.ConvertCharsetToMysql(modelCol.Charset.Value);
            if (!dbCol.CharacterSet.Equals(expectedCharset, StringComparison.OrdinalIgnoreCase))
            {
                _logger?.Debug($"Column '{modelCol.ColumnName}' charset mismatch. Model: '{expectedCharset}', DB: '{dbCol.CharacterSet}'");
                return true;
            }
        }

        // 7. Compare Comment
        var normalizedModelComment = modelCol.Comment ?? "";
        var normalizedDbComment = dbCol.Comment ?? "";
        if (!normalizedModelComment.Equals(normalizedDbComment))
        {
            _logger?.Debug($"Column '{modelCol.ColumnName}' comment mismatch. Model: '{normalizedModelComment}', DB: '{normalizedDbComment}'");
            return true;
        }

        return false; // No changes detected
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
    /// Creates indexes for a new table by calling the main sync method.
    /// </summary>
    private async Task CreateIndexesAsync(
        MySqlConnection connection,
        string tableName,
        Type modelType,
        List<SchemaAnalyzer.ModelColumnDefinition> columns)
    {
        _logger?.Info($"Creating indexes for new table '{tableName}'.");
        await SyncIndexesAsync(connection, tableName, modelType, columns);
    }

    /// <summary>
    /// Synchronizes indexes for existing tables.
    /// </summary>
    private async Task SyncIndexesAsync(
        MySqlConnection connection,
        string tableName,
        Type modelType,
        List<SchemaAnalyzer.ModelColumnDefinition> columns)
    {
        var existingIndexes = (await _schemaAnalyzer.GetTableIndexesAsync(connection, tableName))
            .Where(i => i.IndexName != "PRIMARY").ToList();
        
        var modelIndexes = GenerateModelIndexDefinitions(tableName, modelType, columns);

        var alterations = new List<string>();

        // Indexes to drop or modify
        foreach (var dbIndex in existingIndexes)
        {
            var modelMatch = modelIndexes.FirstOrDefault(m => m.IndexName == dbIndex.IndexName);

            if (modelMatch.IndexName == null) // Check against a property of the struct, not the struct itself
            {
                alterations.Add($"DROP INDEX `{dbIndex.IndexName}`");
            }
            else if (modelMatch.IsUnique != dbIndex.IsUnique || !modelMatch.Columns.SequenceEqual(dbIndex.Columns))
            {
                alterations.Add($"DROP INDEX `{dbIndex.IndexName}`");
                alterations.Add($"ADD {(modelMatch.IsUnique ? "UNIQUE" : "")} INDEX `{modelMatch.IndexName}` ({string.Join(", ", modelMatch.Columns.Select(c => $"`{c}`"))})");
            }
        }

        // Indexes to create
        foreach (var modelIndex in modelIndexes)
        {
            if (!existingIndexes.Any(db => db.IndexName == modelIndex.IndexName))
            {
                alterations.Add($"ADD {(modelIndex.IsUnique ? "UNIQUE" : "")} INDEX `{modelIndex.IndexName}` ({string.Join(", ", modelIndex.Columns.Select(c => $"`{c}`"))})");
            }
        }

        if (!alterations.Any())
        {
            _logger?.Info($"No index changes required for table '{tableName}'.");
            return;
        }

        var alterSql = $"ALTER TABLE `{tableName}` " + string.Join(", ", alterations) + ";";
        _logger?.Info($"Applying index changes to table '{tableName}':\n{alterSql}");

        using var cmd = new MySqlCommand(alterSql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private List<(string IndexName, bool IsUnique, List<string> Columns)> GenerateModelIndexDefinitions(string tableName, Type modelType, List<SchemaAnalyzer.ModelColumnDefinition> columns)
    {
        var indexes = new List<(string IndexName, bool IsUnique, List<string> Columns)>();

        // Single column indexes from [Column] attribute
        foreach (var col in columns)
        {
            if (col.Index)
            {
                indexes.Add(($"idx_{tableName}_{col.ColumnName}", false, new List<string> { col.ColumnName }));
            }
            if (col.Unique && !col.Primary)
            {
                indexes.Add(($"uq_{tableName}_{col.ColumnName}", true, new List<string> { col.ColumnName }));
            }
        }

        // Composite indexes from [CompositeIndex] attribute
        var compositeIndexes = modelType.GetCustomAttributes<CompositeIndexAttribute>();
        foreach (var indexAttr in compositeIndexes)
        {
            indexes.Add((indexAttr.IndexName, indexAttr.Unique, indexAttr.ColumnNames.ToList()));
        }

        return indexes;
    }

    private async Task SyncForeignKeysAsync(MySqlConnection connection, string tableName, Type modelType)
    {
        _logger?.Info($"Syncing foreign keys for table '{tableName}'.");
        var dbForeignKeys = await GetRelatedForeignKeysAsync(connection, tableName);
        var modelForeignKeys = GenerateModelForeignKeyDefinitions(modelType);

        var alterations = new List<string>();

        // Foreign keys to drop
        foreach (var dbFk in dbForeignKeys)
        {
            if (!modelForeignKeys.Any(m => m.ConstraintName == dbFk.ConstraintName))
            {
                alterations.Add($"DROP FOREIGN KEY `{dbFk.ConstraintName}`");
            }
        }

        // Foreign keys to add or modify
        foreach (var modelFk in modelForeignKeys)
        {
            var dbMatch = dbForeignKeys.FirstOrDefault(db => db.ConstraintName == modelFk.ConstraintName);
            if (dbMatch == null)
            {
                alterations.Add($"ADD CONSTRAINT `{modelFk.ConstraintName}` FOREIGN KEY (`{modelFk.ColumnName}`) REFERENCES `{modelFk.ReferencedTableName}` (`{modelFk.ReferencedColumnName}`) ON DELETE {modelFk.OnDelete} ON UPDATE {modelFk.OnUpdate}");
            }
            else if (dbMatch.ColumnName != modelFk.ColumnName || 
                     dbMatch.ReferencedTableName != modelFk.ReferencedTableName || 
                     dbMatch.ReferencedColumnName != modelFk.ReferencedColumnName || 
                     dbMatch.OnDelete.ToString() != modelFk.OnDelete || 
                     dbMatch.OnUpdate.ToString() != modelFk.OnUpdate)
            {
                alterations.Add($"DROP FOREIGN KEY `{dbMatch.ConstraintName}`");
                alterations.Add($"ADD CONSTRAINT `{modelFk.ConstraintName}` FOREIGN KEY (`{modelFk.ColumnName}`) REFERENCES `{modelFk.ReferencedTableName}` (`{modelFk.ReferencedColumnName}`) ON DELETE {modelFk.OnDelete} ON UPDATE {modelFk.OnUpdate}");
            }
        }

        if (!alterations.Any())
        {
            _logger?.Info($"No foreign key changes required for table '{tableName}'.");
            return;
        }

        var alterSql = $"ALTER TABLE `{tableName}` " + string.Join(", ", alterations) + ";";
        _logger?.Info($"Applying foreign key changes to table '{tableName}':\n{alterSql}");

        using var cmd = new MySqlCommand(alterSql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private List<ForeignKeyInfo> GenerateModelForeignKeyDefinitions(Type modelType)
    {
        var fks = new List<ForeignKeyInfo>();
        var tableName = GetTableName(modelType);

        foreach (var prop in modelType.GetProperties())
        {
            var fkAttr = prop.GetCustomAttribute<ForeignKeyAttribute>();
            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();

            if (fkAttr != null && colAttr != null)
            {
                var fk = new ForeignKeyInfo
                {
                    ConstraintName = fkAttr.ConstraintName ?? $"fk_{tableName}_{colAttr.Name ?? prop.Name}",
                    TableName = tableName,
                    ColumnName = colAttr.Name ?? prop.Name,
                    ReferencedTableName = fkAttr.ReferenceTable,
                    ReferencedColumnName = fkAttr.ReferenceColumn,
                    OnUpdate = fkAttr.OnUpdate.ToString().ToUpper(),
                    OnDelete = fkAttr.OnDelete.ToString().ToUpper()
                };
                fks.Add(fk);
            }
        }
        return fks;
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
