using CodeLogic.Abstractions;
using CL.SQLite.Models;
using System.Reflection;

namespace CL.SQLite.Services;

/// <summary>
/// Synchronizes C# model definitions with SQLite database tables.
/// Handles table creation, schema synchronization, and migration tracking.
/// </summary>
public class TableSyncService
{
    private readonly ConnectionManager _connectionManager;
    private readonly SchemaAnalyzer _schemaAnalyzer;
    private readonly MigrationTracker _migrationTracker;
    private readonly ILogger? _logger;

    public TableSyncService(
        ConnectionManager connectionManager,
        string? dataDirectory = null,
        ILogger? logger = null)
    {
        _connectionManager = connectionManager;
        _logger = logger;
        _schemaAnalyzer = new SchemaAnalyzer(logger);
        _migrationTracker = new MigrationTracker(dataDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"), logger);
    }

    /// <summary>
    /// Gets the migration tracker for accessing migration history.
    /// </summary>
    public MigrationTracker MigrationTracker => _migrationTracker;

    /// <summary>
    /// Synchronizes a single model type with its database table.
    /// </summary>
    public async Task<bool> SyncTableAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var modelType = typeof(T);
            var tableName = GetTableName(modelType);

            _logger?.Info($"Starting table synchronization for model '{modelType.Name}' (table: '{tableName}')");

            return await SyncTableInternalAsync(modelType, tableName, cancellationToken);
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
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, bool>();

        _logger?.Info($"Starting batch table synchronization for {modelTypes.Length} model(s)");

        foreach (var modelType in modelTypes)
        {
            var tableName = GetTableName(modelType);

            try
            {
                var result = await SyncTableInternalAsync(modelType, tableName, cancellationToken);
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
        bool includeDerivedNamespaces = false,
        CancellationToken cancellationToken = default)
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
                    t.GetCustomAttribute<SQLiteTableAttribute>() != null)
                .ToArray();

            if (!modelTypes.Any())
            {
                _logger?.Warning($"No model classes with [SQLiteTable] attribute found in namespace '{namespaceName}'");
                return new Dictionary<string, bool>();
            }

            _logger?.Info($"Found {modelTypes.Length} model(s) in namespace '{namespaceName}'. Starting synchronization...");

            return await SyncTablesAsync(modelTypes, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to sync namespace '{namespaceName}': {ex.Message}", ex);
            return new Dictionary<string, bool>();
        }
    }

    /// <summary>
    /// Gets synchronization status for a table.
    /// </summary>
    public async Task<SyncStatus> GetSyncStatusAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var modelType = typeof(T);
            var tableName = GetTableName(modelType);

            return await GetSyncStatusInternalAsync(modelType, tableName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to get sync status for '{typeof(T).Name}': {ex.Message}", ex);
            return new SyncStatus { IsInSync = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Internal implementation of table synchronization.
    /// </summary>
    private async Task<bool> SyncTableInternalAsync(
        Type modelType,
        string tableName,
        CancellationToken cancellationToken)
    {
        return await _connectionManager.ExecuteAsync(async connection =>
        {
            try
            {
                var modelColumns = _schemaAnalyzer.GenerateModelColumnDefinitions(modelType);
                var tableAttr = modelType.GetCustomAttribute<SQLiteTableAttribute>();
                var tableExists = await _schemaAnalyzer.TableExistsAsync(connection, tableName);

                if (!tableExists)
                {
                    // Create new table
                    var createSql = _schemaAnalyzer.GenerateCreateTableStatement(tableName, modelColumns, tableAttr);

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = createSql;
                    await cmd.ExecuteNonQueryAsync(cancellationToken);

                    _logger?.Info($"Created table '{tableName}' with {modelColumns.Count} column(s)");

                    // Record migration
                    await _migrationTracker.RecordMigrationAsync(
                        tableName,
                        "CREATE",
                        $"Created table with {modelColumns.Count} column(s)",
                        true,
                        null,
                        createSql);

                    return true;
                }
                else
                {
                    // Table exists, check for schema differences
                    var dbColumns = await _schemaAnalyzer.GetDatabaseColumnsAsync(connection, tableName);
                    var differences = _schemaAnalyzer.DetectDifferences(modelColumns, dbColumns);

                    if (differences.HasDifferences)
                    {
                        _logger?.Info($"Schema differences detected for table '{tableName}':");

                        // Add missing columns
                        foreach (var missingCol in differences.MissingColumns)
                        {
                            var alterSql = GenerateAddColumnStatement(tableName, missingCol);
                            try
                            {
                                using var cmd = connection.CreateCommand();
                                cmd.CommandText = alterSql;
                                await cmd.ExecuteNonQueryAsync(cancellationToken);
                                _logger?.Info($"  Added column '{missingCol.ColumnName}' to table '{tableName}'");

                                // Record migration
                                await _migrationTracker.RecordMigrationAsync(
                                    tableName,
                                    "ALTER",
                                    $"Added column '{missingCol.ColumnName}'",
                                    true,
                                    null,
                                    alterSql);
                            }
                            catch (Exception ex)
                            {
                                _logger?.Error($"  Failed to add column '{missingCol.ColumnName}': {ex.Message}", ex);

                                // Record failed migration
                                await _migrationTracker.RecordMigrationAsync(
                                    tableName,
                                    "ALTER",
                                    $"Add column '{missingCol.ColumnName}' (FAILED)",
                                    false,
                                    ex.Message,
                                    alterSql);
                            }
                        }

                        // Log extra columns (but don't drop them automatically)
                        foreach (var extraCol in differences.ExtraColumns)
                        {
                            _logger?.Warning($"  Extra column '{extraCol.ColumnName}' found in table '{tableName}' (not in model)");
                        }

                        // Log modified columns
                        foreach (var (modelCol, dbCol) in differences.ModifiedColumns)
                        {
                            _logger?.Warning($"  Column '{modelCol.ColumnName}' definition differs: model={modelCol.DataType}, db={dbCol.DataType}");
                        }

                        return true;
                    }
                    else
                    {
                        _logger?.Info($"Table '{tableName}' schema is in sync with model definition");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error synchronizing table '{tableName}': {ex.Message}", ex);
                return false;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Gets synchronization status without making changes.
    /// </summary>
    private async Task<SyncStatus> GetSyncStatusInternalAsync(
        Type modelType,
        string tableName,
        CancellationToken cancellationToken)
    {
        return await _connectionManager.ExecuteAsync(async connection =>
        {
            try
            {
                var modelColumns = _schemaAnalyzer.GenerateModelColumnDefinitions(modelType);
                var tableExists = await _schemaAnalyzer.TableExistsAsync(connection, tableName);

                if (!tableExists)
                {
                    return new SyncStatus
                    {
                        IsInSync = false,
                        TableExists = false,
                        Message = $"Table '{tableName}' does not exist in database"
                    };
                }

                var dbColumns = await _schemaAnalyzer.GetDatabaseColumnsAsync(connection, tableName);
                var differences = _schemaAnalyzer.DetectDifferences(modelColumns, dbColumns);

                return new SyncStatus
                {
                    IsInSync = !differences.HasDifferences,
                    TableExists = true,
                    ColumnCount = dbColumns.Count,
                    MissingColumnsCount = differences.MissingColumns.Count,
                    ExtraColumnsCount = differences.ExtraColumns.Count,
                    ModifiedColumnsCount = differences.ModifiedColumns.Count,
                    Message = differences.HasDifferences ? "Schema differences detected" : "Schema is in sync"
                };
            }
            catch (Exception ex)
            {
                return new SyncStatus { IsInSync = false, ErrorMessage = ex.Message };
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Generates ALTER TABLE ADD COLUMN statement.
    /// </summary>
    private string GenerateAddColumnStatement(
        string tableName,
        SchemaAnalyzer.ModelColumnDefinition column)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"ALTER TABLE {tableName} ADD COLUMN ");

        // Reuse the column definition generation logic
        var columnDef = GenerateColumnDefinitionForAlter(column);
        sb.Append(columnDef);
        sb.Append(";");

        return sb.ToString();
    }

    /// <summary>
    /// Generates column definition for ALTER TABLE.
    /// </summary>
    private string GenerateColumnDefinitionForAlter(SchemaAnalyzer.ModelColumnDefinition col)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"{col.ColumnName} ");

        var dataTypeMap = new Dictionary<SQLiteDataType, string>
        {
            { SQLiteDataType.INTEGER, "INTEGER" },
            { SQLiteDataType.REAL, "REAL" },
            { SQLiteDataType.TEXT, "TEXT" },
            { SQLiteDataType.BLOB, "BLOB" },
            { SQLiteDataType.NUMERIC, "NUMERIC" },
            { SQLiteDataType.BOOLEAN, "BOOLEAN" },
            { SQLiteDataType.DATE, "DATE" },
            { SQLiteDataType.DATETIME, "DATETIME" }
        };

        sb.Append(dataTypeMap.TryGetValue(col.DataType, out var type) ? type : "TEXT");

        if (col.NotNull)
        {
            sb.Append(" NOT NULL");
        }

        if (!string.IsNullOrEmpty(col.DefaultValue))
        {
            if (col.DefaultValue.Equals("CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(" DEFAULT CURRENT_TIMESTAMP");
            }
            else
            {
                sb.Append($" DEFAULT {col.DefaultValue}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets the table name from a model type.
    /// </summary>
    private string GetTableName(Type modelType)
    {
        var tableAttr = modelType.GetCustomAttribute<SQLiteTableAttribute>();
        return tableAttr?.TableName ?? modelType.Name.ToLower();
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
                    types = types.Where(t => t.Namespace!.StartsWith(namespaceName));
                }
                else
                {
                    types = types.Where(t => t.Namespace == namespaceName);
                }

                query.AddRange(types);
            }
            catch
            {
                // Ignore assembly loading errors
            }
        }

        return query;
    }
}

/// <summary>
/// Represents the synchronization status of a table.
/// </summary>
public class SyncStatus
{
    public bool IsInSync { get; set; }
    public bool TableExists { get; set; }
    public int ColumnCount { get; set; }
    public int MissingColumnsCount { get; set; }
    public int ExtraColumnsCount { get; set; }
    public int ModifiedColumnsCount { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}
