using CodeLogic.Abstractions;
using CL.MySQL2.Models;
using MySqlConnector;
using System.Reflection;
using System.Text;

namespace CL.MySQL2.Services;

/// <summary>
/// Analyzes and compares database schemas with C# model definitions.
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
        public DataType DataType { get; set; }
        public int Size { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }
        public bool Primary { get; set; }
        public bool AutoIncrement { get; set; }
        public bool NotNull { get; set; }
        public bool Unique { get; set; }
        public bool Index { get; set; }
        public string? DefaultValue { get; set; }
        public Charset? Charset { get; set; }
        public bool Unsigned { get; set; }
        public bool OnUpdateCurrentTimestamp { get; set; }
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Represents a column definition from the database.
    /// </summary>
    public class DatabaseColumnDefinition
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? ColumnType { get; set; }
        public bool Nullable { get; set; }
        public string? ColumnKey { get; set; }
        public bool AutoIncrement { get; set; }
        public string? DefaultValue { get; set; }
        public string? Extra { get; set; }
        public string? CharacterSet { get; set; }
        public string? Collation { get; set; }
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Generates column definitions from a model type based on ColumnAttribute.
    /// </summary>
    public List<ModelColumnDefinition> GenerateModelColumnDefinitions(Type modelType)
    {
        var columns = new List<ModelColumnDefinition>();

        var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            // Skip ignored properties
            if (property.GetCustomAttribute<IgnoreAttribute>() != null)
                continue;

            var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr == null)
                continue;

            var columnDef = new ModelColumnDefinition
            {
                PropertyName = property.Name,
                ColumnName = columnAttr.Name ?? property.Name,
                DataType = columnAttr.DataType,
                Size = columnAttr.Size,
                Precision = columnAttr.Precision,
                Scale = columnAttr.Scale,
                Primary = columnAttr.Primary,
                AutoIncrement = columnAttr.AutoIncrement,
                NotNull = columnAttr.NotNull,
                Unique = columnAttr.Unique,
                Index = columnAttr.Index,
                DefaultValue = columnAttr.DefaultValue,
                Charset = columnAttr.Charset,
                Unsigned = columnAttr.Unsigned,
                OnUpdateCurrentTimestamp = columnAttr.OnUpdateCurrentTimestamp,
                Comment = columnAttr.Comment
            };

            columns.Add(columnDef);
        }

        return columns;
    }

    /// <summary>
    /// Retrieves existing columns from a database table.
    /// </summary>
    public async Task<List<DatabaseColumnDefinition>> GetDatabaseColumnsAsync(
        MySqlConnection connection,
        string tableName)
    {
        var columns = new List<DatabaseColumnDefinition>();

        try
        {
            using var cmd = new MySqlCommand($"SHOW FULL COLUMNS FROM `{tableName}`", connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var columnType = reader.GetString(1);  // Type column
                var columnDef = new DatabaseColumnDefinition
                {
                    ColumnName = reader.GetString(0),  // Field column
                    ColumnType = columnType,
                    DataType = columnType.Split('(')[0].ToUpper(),
                    Nullable = reader.GetString(3) == "YES",  // Null column
                    ColumnKey = reader.IsDBNull(4) ? null : reader.GetString(4),  // Key column
                    DefaultValue = reader.IsDBNull(5) ? null : reader.GetString(5),  // Default column
                    Extra = reader.IsDBNull(6) ? null : reader.GetString(6),  // Extra column
                    CharacterSet = reader.IsDBNull(2) ? null : reader.GetString(2),  // Collation column
                    Comment = reader.IsDBNull(8) ? null : reader.GetString(8)  // Comment column
                };

                columnDef.AutoIncrement = columnDef.Extra?.Contains("auto_increment", StringComparison.OrdinalIgnoreCase) ?? false;

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
    public async Task<bool> TableExistsAsync(MySqlConnection connection, string tableName)
    {
        try
        {
            using var cmd = new MySqlCommand(
                $"SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName",
                connection);
            cmd.Parameters.AddWithValue("@tableName", tableName);

            var result = await cmd.ExecuteScalarAsync();
            return result != null && Convert.ToInt32(result) == 1;
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
        MySqlConnection connection,
        string tableName)
    {
        var indexes = new List<(string IndexName, bool IsUnique, List<string> Columns)>();

        try
        {
            using var cmd = new MySqlCommand($"SHOW INDEXES FROM `{tableName}`", connection);
            using var reader = await cmd.ExecuteReaderAsync();

            var indexMap = new Dictionary<string, (bool IsUnique, List<string> Columns)>();

            while (await reader.ReadAsync())
            {
                var indexName = reader.GetString("Key_name");
                var columnName = reader.GetString("Column_name");
                var isUnique = reader.GetInt32("Non_unique") == 0;

                if (!indexMap.ContainsKey(indexName))
                {
                    indexMap[indexName] = (isUnique, new List<string>());
                }

                indexMap[indexName].Columns.Add(columnName);
            }

            foreach (var kvp in indexMap)
            {
                indexes.Add((kvp.Key, kvp.Value.IsUnique, kvp.Value.Columns));
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
        TableAttribute? tableAttr = null)
    {
        if (!columns.Any())
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE IF NOT EXISTS `{tableName}` (");

        var columnDefs = new List<string>();

        foreach (var col in columns)
        {
            var colDef = GenerateColumnDefinition(col);
            columnDefs.Add($"  {colDef}");
        }

        // Add primary key if exists
        var primaryKey = columns.FirstOrDefault(c => c.Primary);
        if (primaryKey != null)
        {
            columnDefs.Add($"  PRIMARY KEY (`{primaryKey.ColumnName}`)");
        }

        sb.Append(string.Join(",\n", columnDefs));
        sb.AppendLine();
        sb.Append(")");

        // Add engine and charset
        var engine = tableAttr?.Engine ?? TableEngine.InnoDB;
        var charset = tableAttr?.Charset ?? Charset.Utf8mb4;

        sb.Append($" ENGINE={engine}");
        sb.Append($" DEFAULT CHARSET={ConvertCharsetToMysql(charset)}");

        if (!string.IsNullOrEmpty(tableAttr?.Collation))
        {
            sb.Append($" COLLATE={tableAttr.Collation}");
        }

        if (!string.IsNullOrEmpty(tableAttr?.Comment))
        {
            sb.Append($" COMMENT='{EscapeSqlString(tableAttr.Comment)}'");
        }

        sb.Append(";");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a single column definition string.
    /// </summary>
    private string GenerateColumnDefinition(ModelColumnDefinition col)
    {
        var sb = new StringBuilder();
        sb.Append($"`{col.ColumnName}` ");
        sb.Append(ConvertDataTypeToMysql(col.DataType, col.Size, col.Precision, col.Scale));

        if (!string.IsNullOrEmpty(col.Charset?.ToString()))
        {
            sb.Append($" CHARACTER SET {ConvertCharsetToMysql(col.Charset.Value)}");
        }

        if (col.Unsigned && IsNumericType(col.DataType))
        {
            sb.Append(" UNSIGNED");
        }

        if (col.AutoIncrement)
        {
            sb.Append(" AUTO_INCREMENT");
        }

        if (col.NotNull)
        {
            sb.Append(" NOT NULL");
        }

        if (!string.IsNullOrEmpty(col.DefaultValue) && col.DefaultValue != "NULL")
        {
            if (col.DefaultValue.Equals("CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(" DEFAULT CURRENT_TIMESTAMP");
            }
            else
            {
                sb.Append($" DEFAULT '{EscapeSqlString(col.DefaultValue)}'");
            }
        }

        if (col.OnUpdateCurrentTimestamp && col.DataType == DataType.Timestamp)
        {
            sb.Append(" ON UPDATE CURRENT_TIMESTAMP");
        }

        if (!string.IsNullOrEmpty(col.Comment))
        {
            sb.Append($" COMMENT '{EscapeSqlString(col.Comment)}'");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts C# DataType enum to MySQL data type string.
    /// </summary>
    private string ConvertDataTypeToMysql(DataType dataType, int size, int precision, int scale)
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
            DataType.Decimal => $"DECIMAL({precision},{scale})",
            DataType.DateTime => "DATETIME",
            DataType.Date => "DATE",
            DataType.Time => "TIME",
            DataType.Timestamp => "TIMESTAMP",
            DataType.Year => "YEAR",
            DataType.Char => $"CHAR({Math.Max(size, 1)})",
            DataType.VarChar => $"VARCHAR({(size > 0 ? size : 255)})",
            DataType.TinyText => "TINYTEXT",
            DataType.Text => "TEXT",
            DataType.MediumText => "MEDIUMTEXT",
            DataType.LongText => "LONGTEXT",
            DataType.Json => "JSON",
            DataType.Binary => $"BINARY({Math.Max(size, 1)})",
            DataType.VarBinary => $"VARBINARY({(size > 0 ? size : 255)})",
            DataType.TinyBlob => "TINYBLOB",
            DataType.Blob => "BLOB",
            DataType.MediumBlob => "MEDIUMBLOB",
            DataType.LongBlob => "LONGBLOB",
            DataType.Uuid => "CHAR(36)",
            DataType.Enum => "ENUM",
            DataType.Set => "SET",
            DataType.Bool => "TINYINT(1)",
            _ => "VARCHAR(255)"
        };
    }

    /// <summary>
    /// Converts C# Charset enum to MySQL charset string.
    /// </summary>
    private string ConvertCharsetToMysql(Charset charset)
    {
        return charset switch
        {
            Charset.Utf8 => "utf8",
            Charset.Utf8mb4 => "utf8mb4",
            Charset.Latin1 => "latin1",
            Charset.Ascii => "ascii",
            Charset.Binary => "binary",
            _ => "utf8mb4"
        };
    }

    /// <summary>
    /// Checks if a DataType is numeric.
    /// </summary>
    private bool IsNumericType(DataType dataType)
    {
        return dataType switch
        {
            DataType.TinyInt or DataType.SmallInt or DataType.MediumInt or
            DataType.Int or DataType.BigInt or DataType.Float or DataType.Double or
            DataType.Decimal => true,
            _ => false
        };
    }

    /// <summary>
    /// Escapes single quotes in SQL strings.
    /// </summary>
    private string EscapeSqlString(string value)
    {
        return value.Replace("'", "''");
    }
}
