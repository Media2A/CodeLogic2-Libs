using CodeLogic.Abstractions;
using CL.SQLite.Models;
using Microsoft.Data.Sqlite;
using System.Reflection;
using System.Text;

namespace CL.SQLite.Services;

/// <summary>
/// Analyzes and compares SQLite database schemas with C# model definitions.
/// </summary>
public class SchemaAnalyzer
{
    private readonly ILogger? _logger;

    public SchemaAnalyzer(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Represents a column definition from the model.
    /// </summary>
    public class ModelColumnDefinition
    {
        public string PropertyName { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public SQLiteDataType DataType { get; set; }
        public bool Primary { get; set; }
        public bool AutoIncrement { get; set; }
        public bool NotNull { get; set; }
        public bool Unique { get; set; }
        public string? DefaultValue { get; set; }
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Represents a column definition from the database.
    /// </summary>
    public class DatabaseColumnDefinition
    {
        public int Cid { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool NotNull { get; set; }
        public string? DefaultValue { get; set; }
        public bool PrimaryKey { get; set; }
    }

    /// <summary>
    /// Generates column definitions from a model type based on SQLiteColumnAttribute.
    /// </summary>
    public List<ModelColumnDefinition> GenerateModelColumnDefinitions(Type modelType)
    {
        var columns = new List<ModelColumnDefinition>();

        var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var columnAttr = property.GetCustomAttribute<SQLiteColumnAttribute>();
            if (columnAttr == null)
                continue;

            var columnDef = new ModelColumnDefinition
            {
                PropertyName = property.Name,
                ColumnName = columnAttr.ColumnName ?? property.Name,
                DataType = columnAttr.DataType,
                Primary = columnAttr.IsPrimaryKey,
                AutoIncrement = columnAttr.IsAutoIncrement,
                NotNull = columnAttr.IsNotNull,
                Unique = columnAttr.IsUnique,
                DefaultValue = columnAttr.DefaultValue,
                Comment = null
            };

            columns.Add(columnDef);
        }

        return columns;
    }

    /// <summary>
    /// Retrieves existing columns from a database table.
    /// </summary>
    public async Task<List<DatabaseColumnDefinition>> GetDatabaseColumnsAsync(
        SqliteConnection connection,
        string tableName)
    {
        var columns = new List<DatabaseColumnDefinition>();

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName})";
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var columnDef = new DatabaseColumnDefinition
                {
                    Cid = reader.GetInt32(0),              // cid
                    ColumnName = reader.GetString(1),      // name
                    DataType = reader.GetString(2),        // type
                    NotNull = reader.GetInt32(3) == 1,     // notnull
                    DefaultValue = reader.IsDBNull(4) ? null : reader.GetString(4),  // dflt_value
                    PrimaryKey = reader.GetInt32(5) == 1   // pk
                };

                columns.Add(columnDef);
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error retrieving columns for table '{tableName}': {ex.Message}", ex);
        }

        return columns;
    }

    /// <summary>
    /// Checks if a table exists in the database.
    /// </summary>
    public async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
    {
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@tableName";
            cmd.Parameters.AddWithValue("@tableName", tableName);

            var result = await cmd.ExecuteScalarAsync();
            return result != null;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error checking if table '{tableName}' exists: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Gets existing indexes for a table.
    /// </summary>
    public async Task<List<(string IndexName, bool IsUnique, List<string> Columns)>> GetTableIndexesAsync(
        SqliteConnection connection,
        string tableName)
    {
        var indexes = new List<(string IndexName, bool IsUnique, List<string> Columns)>();

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA index_list({tableName})";
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var indexName = reader.GetString(1);  // name
                var isUnique = reader.GetInt32(2) == 1;  // unique

                // Get columns for this index
                using var colCmd = connection.CreateCommand();
                colCmd.CommandText = $"PRAGMA index_info({indexName})";
                using var colReader = await colCmd.ExecuteReaderAsync();

                var indexColumns = new List<string>();
                while (await colReader.ReadAsync())
                {
                    indexColumns.Add(colReader.GetString(2)); // name
                }

                if (indexColumns.Any())
                {
                    indexes.Add((indexName, isUnique, indexColumns));
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error retrieving indexes for table '{tableName}': {ex.Message}", ex);
        }

        return indexes;
    }

    /// <summary>
    /// Generates SQL CREATE TABLE statement from model definition.
    /// </summary>
    public string GenerateCreateTableStatement(
        string tableName,
        List<ModelColumnDefinition> columns,
        SQLiteTableAttribute? tableAttr = null)
    {
        if (!columns.Any())
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {tableName} (");

        var columnDefs = new List<string>();

        foreach (var col in columns)
        {
            var colDef = GenerateColumnDefinition(col);
            columnDefs.Add($"  {colDef}");
        }

        sb.Append(string.Join(",\n", columnDefs));
        sb.AppendLine();
        sb.Append(");");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a single column definition string.
    /// </summary>
    private string GenerateColumnDefinition(ModelColumnDefinition col)
    {
        var sb = new StringBuilder();
        sb.Append($"{col.ColumnName} ");
        sb.Append(ConvertDataTypeToSQLite(col.DataType));

        if (col.Primary && col.AutoIncrement)
        {
            sb.Append(" PRIMARY KEY AUTOINCREMENT");
        }
        else if (col.Primary)
        {
            sb.Append(" PRIMARY KEY");
        }
        else if (col.Unique)
        {
            sb.Append(" UNIQUE");
        }

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
    /// Converts C# SQLiteDataType enum to SQLite data type string.
    /// </summary>
    private string ConvertDataTypeToSQLite(SQLiteDataType dataType)
    {
        return dataType switch
        {
            SQLiteDataType.INTEGER => "INTEGER",
            SQLiteDataType.REAL => "REAL",
            SQLiteDataType.TEXT => "TEXT",
            SQLiteDataType.BLOB => "BLOB",
            SQLiteDataType.NUMERIC => "NUMERIC",
            SQLiteDataType.BOOLEAN => "BOOLEAN",
            SQLiteDataType.DATE => "DATE",
            SQLiteDataType.DATETIME => "DATETIME",
            _ => "TEXT"
        };
    }

    /// <summary>
    /// Detects differences between model definition and database schema.
    /// </summary>
    public SchemaDifferences DetectDifferences(
        List<ModelColumnDefinition> modelColumns,
        List<DatabaseColumnDefinition> dbColumns)
    {
        var differences = new SchemaDifferences();

        var dbColumnMap = dbColumns.ToDictionary(c => c.ColumnName.ToLower());
        var modelColumnMap = modelColumns.ToDictionary(c => c.ColumnName.ToLower());

        // Find missing columns in database
        foreach (var modelCol in modelColumns)
        {
            if (!dbColumnMap.ContainsKey(modelCol.ColumnName.ToLower()))
            {
                differences.MissingColumns.Add(modelCol);
            }
        }

        // Find extra columns in database
        foreach (var dbCol in dbColumns)
        {
            if (!modelColumnMap.ContainsKey(dbCol.ColumnName.ToLower()))
            {
                differences.ExtraColumns.Add(dbCol);
            }
        }

        // Find modified columns
        foreach (var modelCol in modelColumns)
        {
            var key = modelCol.ColumnName.ToLower();
            if (dbColumnMap.TryGetValue(key, out var dbCol))
            {
                if (HasColumnChanged(modelCol, dbCol))
                {
                    differences.ModifiedColumns.Add((modelCol, dbCol));
                }
            }
        }

        return differences;
    }

    /// <summary>
    /// Determines if a column definition has changed.
    /// </summary>
    private bool HasColumnChanged(ModelColumnDefinition modelCol, DatabaseColumnDefinition dbCol)
    {
        var expectedType = ConvertDataTypeToSQLite(modelCol.DataType);
        if (dbCol.DataType != expectedType)
            return true;

        if (modelCol.NotNull != dbCol.NotNull)
            return true;

        if (modelCol.Primary != dbCol.PrimaryKey)
            return true;

        return false;
    }

    /// <summary>
    /// Schema differences result.
    /// </summary>
    public class SchemaDifferences
    {
        public List<ModelColumnDefinition> MissingColumns { get; } = new();
        public List<DatabaseColumnDefinition> ExtraColumns { get; } = new();
        public List<(ModelColumnDefinition Model, DatabaseColumnDefinition Database)> ModifiedColumns { get; } = new();

        public bool HasDifferences =>
            MissingColumns.Any() ||
            ExtraColumns.Any() ||
            ModifiedColumns.Any();
    }
}
