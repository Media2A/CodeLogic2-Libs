using CL.MySQL2.Models;
using System.Text;

namespace CL.MySQL2.Core;

/// <summary>
/// Provides type conversion between C# types and MySQL data types.
/// Optimized for performance with minimal allocations.
/// </summary>
public static class TypeConverter
{
    /// <summary>
    /// Converts a C# value to a MySQL-compatible format based on the specified data type.
    /// </summary>
    public static object ToMySql(object? value, DataType dataType)
    {
        if (value == null || value == DBNull.Value)
            return DBNull.Value;

        return dataType switch
        {
            DataType.DateTime => value switch
            {
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                DateTimeOffset dto => dto.DateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                string s => s,
                _ => value.ToString() ?? string.Empty
            },

            DataType.Date => value switch
            {
                DateTime dt => dt.ToString("yyyy-MM-dd"),
                DateOnly d => d.ToString("yyyy-MM-dd"),
                DateTimeOffset dto => dto.DateTime.ToString("yyyy-MM-dd"),
                string s => s,
                _ => value.ToString() ?? string.Empty
            },

            DataType.Time => value switch
            {
                TimeSpan ts => ts.ToString(@"hh\:mm\:ss"),
                TimeOnly t => t.ToString("HH:mm:ss"),
                DateTime dt => dt.ToString("HH:mm:ss"),
                string s => s,
                _ => value.ToString() ?? string.Empty
            },

            DataType.Timestamp => value switch
            {
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                DateTimeOffset dto => dto.DateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                string s => s,
                _ => value.ToString() ?? string.Empty
            },

            DataType.Binary or DataType.VarBinary => value switch
            {
                Guid g => g.ToByteArray(),
                byte[] b => b,
                string s => Encoding.UTF8.GetBytes(s),
                _ => value
            },

            DataType.Uuid => value switch
            {
                Guid g => g.ToString(),
                string s => s,
                byte[] b when b.Length == 16 => new Guid(b).ToString(),
                _ => value.ToString() ?? string.Empty
            },

            DataType.Bool => value switch
            {
                bool b => b ? (byte)1 : (byte)0,
                int i => (byte)(i != 0 ? 1 : 0),
                _ => Convert.ToByte(value)
            },

            DataType.Json => value switch
            {
                string s => s,
                _ => System.Text.Json.JsonSerializer.Serialize(value)
            },

            DataType.TinyInt or DataType.SmallInt or DataType.MediumInt or DataType.Int or DataType.BigInt => value switch
            {
                Enum e => Convert.ToInt64(e),
                bool b => b ? 1 : 0,
                _ => value
            },

            DataType.Float or DataType.Double or DataType.Decimal => value switch
            {
                string s when decimal.TryParse(s, out var dec) => dec,
                _ => value
            },

            DataType.VarChar or DataType.Char or DataType.Text or DataType.TinyText or DataType.MediumText or DataType.LongText =>
                value.ToString() ?? string.Empty,

            DataType.Blob or DataType.TinyBlob or DataType.MediumBlob or DataType.LongBlob => value switch
            {
                byte[] b => b,
                string s => Encoding.UTF8.GetBytes(s),
                _ => value
            },

            _ => value
        };
    }

    /// <summary>
    /// Converts a MySQL value back to a C#-compatible format.
    /// </summary>
    public static object? FromMySql(object value, DataType dataType, Type targetType)
    {
        if (value == null || value == DBNull.Value)
            return GetDefaultValue(targetType);

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return dataType switch
        {
            DataType.DateTime or DataType.Timestamp => ConvertToDateTime(value, underlyingType),
            DataType.Date => ConvertToDate(value, underlyingType),
            DataType.Time => ConvertToTime(value, underlyingType),
            DataType.Binary or DataType.VarBinary => ConvertToBinary(value, underlyingType),
            DataType.Uuid => ConvertToGuid(value, underlyingType),
            DataType.Bool => ConvertToBoolean(value, underlyingType),
            DataType.Json => ConvertFromJson(value, underlyingType),
            DataType.TinyInt or DataType.SmallInt or DataType.MediumInt or DataType.Int or DataType.BigInt =>
                ConvertToInteger(value, underlyingType),
            DataType.Float or DataType.Double or DataType.Decimal =>
                ConvertToDecimal(value, underlyingType),
            _ => ConvertToType(value, underlyingType)
        };
    }

    private static object? ConvertToDateTime(object value, Type targetType)
    {
        if (targetType == typeof(DateTime))
        {
            return value switch
            {
                DateTime dt => dt,
                MySqlConnector.MySqlDateTime mdt => mdt.GetDateTime(),
                string s when DateTime.TryParse(s, out var dt) => dt,
                _ => DateTime.MinValue
            };
        }
        else if (targetType == typeof(DateTimeOffset))
        {
            return value switch
            {
                DateTimeOffset dto => dto,
                DateTime dt => new DateTimeOffset(dt),
                MySqlConnector.MySqlDateTime mdt => new DateTimeOffset(mdt.GetDateTime()),
                string s when DateTimeOffset.TryParse(s, out var dto) => dto,
                _ => DateTimeOffset.MinValue
            };
        }
        return ConvertToType(value, targetType);
    }

    private static object? ConvertToDate(object value, Type targetType)
    {
        if (targetType == typeof(DateOnly))
        {
            return value switch
            {
                DateOnly d => d,
                DateTime dt => DateOnly.FromDateTime(dt),
                string s when DateOnly.TryParse(s, out var d) => d,
                _ => DateOnly.MinValue
            };
        }
        else if (targetType == typeof(DateTime))
        {
            return value switch
            {
                DateTime dt => dt.Date,
                DateOnly d => d.ToDateTime(TimeOnly.MinValue),
                string s when DateTime.TryParse(s, out var dt) => dt.Date,
                _ => DateTime.MinValue
            };
        }
        return ConvertToType(value, targetType);
    }

    private static object? ConvertToTime(object value, Type targetType)
    {
        if (targetType == typeof(TimeSpan))
        {
            return value switch
            {
                TimeSpan ts => ts,
                TimeOnly t => t.ToTimeSpan(),
                string s when TimeSpan.TryParse(s, out var ts) => ts,
                _ => TimeSpan.Zero
            };
        }
        else if (targetType == typeof(TimeOnly))
        {
            return value switch
            {
                TimeOnly t => t,
                TimeSpan ts => TimeOnly.FromTimeSpan(ts),
                string s when TimeOnly.TryParse(s, out var t) => t,
                _ => TimeOnly.MinValue
            };
        }
        return ConvertToType(value, targetType);
    }

    private static object? ConvertToBinary(object value, Type targetType)
    {
        if (targetType == typeof(Guid))
        {
            return value switch
            {
                Guid g => g,
                byte[] b when b.Length == 16 => new Guid(b),
                string s when Guid.TryParse(s, out var g) => g,
                _ => Guid.Empty
            };
        }
        else if (targetType == typeof(byte[]))
        {
            return value switch
            {
                byte[] b => b,
                Guid g => g.ToByteArray(),
                string s => Encoding.UTF8.GetBytes(s),
                _ => Array.Empty<byte>()
            };
        }
        return ConvertToType(value, targetType);
    }

    private static object? ConvertToGuid(object value, Type targetType)
    {
        if (targetType == typeof(Guid))
        {
            return value switch
            {
                Guid g => g,
                string s when Guid.TryParse(s, out var g) => g,
                byte[] b when b.Length == 16 => new Guid(b),
                _ => Guid.Empty
            };
        }
        return ConvertToType(value, targetType);
    }

    private static object? ConvertToBoolean(object value, Type targetType)
    {
        if (targetType == typeof(bool))
        {
            return value switch
            {
                bool b => b,
                byte by => by != 0,
                int i => i != 0,
                long l => l != 0,
                string s => s.Equals("1") || s.Equals("true", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
        return ConvertToType(value, targetType);
    }

    private static object? ConvertFromJson(object value, Type targetType)
    {
        if (targetType == typeof(string))
            return value.ToString();

        try
        {
            var jsonString = value.ToString();
            if (jsonString != null)
                return System.Text.Json.JsonSerializer.Deserialize(jsonString, targetType);
        }
        catch
        {
            // Fallthrough to default value
        }

        return GetDefaultValue(targetType);
    }

    private static object? ConvertToInteger(object value, Type targetType)
    {
        if (targetType.IsEnum)
        {
            return value switch
            {
                string s => Enum.Parse(targetType, s, true),
                _ => Enum.ToObject(targetType, value)
            };
        }

        return Convert.ChangeType(value, targetType);
    }

    private static object? ConvertToDecimal(object value, Type targetType)
    {
        return value switch
        {
            decimal d => ConvertToType(d, targetType),
            double db => ConvertToType(db, targetType),
            float f => ConvertToType(f, targetType),
            string s when decimal.TryParse(s, out var dec) => ConvertToType(dec, targetType),
            _ => ConvertToType(value, targetType)
        };
    }

    private static object? ConvertToType(object value, Type targetType)
    {
        try
        {
            if (targetType == typeof(string))
                return value.ToString();

            if (targetType == typeof(object))
                return value;

            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return GetDefaultValue(targetType);
        }
    }

    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    /// <summary>
    /// Gets the MySQL type string for a given DataType.
    /// </summary>
    public static string GetMySqlTypeString(DataType dataType, int size = 0, int precision = 10, int scale = 2)
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
            DataType.Char => size > 0 ? $"CHAR({size})" : "CHAR(255)",
            DataType.VarChar => size > 0 ? $"VARCHAR({size})" : "VARCHAR(255)",
            DataType.TinyText => "TINYTEXT",
            DataType.Text => "TEXT",
            DataType.MediumText => "MEDIUMTEXT",
            DataType.LongText => "LONGTEXT",
            DataType.Json => "JSON",
            DataType.Binary => size > 0 ? $"BINARY({size})" : "BINARY(16)",
            DataType.VarBinary => size > 0 ? $"VARBINARY({size})" : "VARBINARY(255)",
            DataType.TinyBlob => "TINYBLOB",
            DataType.Blob => "BLOB",
            DataType.MediumBlob => "MEDIUMBLOB",
            DataType.LongBlob => "LONGBLOB",
            DataType.Uuid => "CHAR(36)",
            DataType.Bool => "TINYINT(1)",
            DataType.Enum => size > 0 ? $"ENUM({size})" : "ENUM",
            DataType.Set => size > 0 ? $"SET({size})" : "SET",
            _ => "VARCHAR(255)"
        };
    }
}
