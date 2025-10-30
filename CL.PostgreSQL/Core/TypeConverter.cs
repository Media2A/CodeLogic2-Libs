using CL.PostgreSQL.Models;
using NpgsqlTypes;
using System.Text;

namespace CL.PostgreSQL.Core;

/// <summary>
/// Provides type conversion between C# types and PostgreSQL data types.
/// Optimized for performance with minimal allocations.
/// </summary>
public static class TypeConverter
{
    /// <summary>
    /// Converts a C# value to a PostgreSQL-compatible format based on the specified data type.
    /// </summary>
    public static object ToPostgreSQL(object? value, DataType dataType)
    {
        if (value == null || value == DBNull.Value)
            return DBNull.Value;

        return dataType switch
        {
            DataType.Timestamp => value switch
            {
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                DateTimeOffset dto => dto.DateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                string s => s,
                _ => value.ToString() ?? string.Empty
            },

            DataType.TimestampTz => value switch
            {
                DateTime dt => new DateTimeOffset(dt).ToString("yyyy-MM-dd HH:mm:ss zzz"),
                DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss zzz"),
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

            DataType.TimeTz => value switch
            {
                TimeSpan ts => ts.ToString(@"hh\:mm\:ss"),
                TimeOnly t => t.ToString("HH:mm:ss"),
                DateTime dt => dt.ToString("HH:mm:ss zzz"),
                DateTimeOffset dto => dto.ToString("HH:mm:ss zzz"),
                string s => s,
                _ => value.ToString() ?? string.Empty
            },

            DataType.Bytea => value switch
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
                bool b => b,
                int i => i != 0,
                byte by => by != 0,
                string s => s.Equals("true", StringComparison.OrdinalIgnoreCase),
                _ => Convert.ToBoolean(value)
            },

            DataType.Json or DataType.Jsonb => value switch
            {
                string s => s,
                _ => System.Text.Json.JsonSerializer.Serialize(value)
            },

            DataType.SmallInt or DataType.Int or DataType.BigInt => value switch
            {
                Enum e => Convert.ToInt64(e),
                bool b => b ? 1 : 0,
                _ => value
            },

            DataType.Real or DataType.DoublePrecision or DataType.Numeric => value switch
            {
                string s when decimal.TryParse(s, out var dec) => dec,
                _ => value
            },

            DataType.VarChar or DataType.Char or DataType.Text =>
                value.ToString() ?? string.Empty,

            DataType.IntArray => value switch
            {
                int[] arr => arr,
                List<int> list => list.ToArray(),
                _ => value
            },

            DataType.BigIntArray => value switch
            {
                long[] arr => arr,
                List<long> list => list.ToArray(),
                _ => value
            },

            DataType.TextArray => value switch
            {
                string[] arr => arr,
                List<string> list => list.ToArray(),
                _ => value
            },

            DataType.NumericArray => value switch
            {
                decimal[] arr => arr,
                List<decimal> list => list.ToArray(),
                _ => value
            },

            _ => value
        };
    }

    /// <summary>
    /// Converts a PostgreSQL value back to a C#-compatible format.
    /// </summary>
    public static object? FromPostgreSQL(object value, DataType dataType, Type targetType)
    {
        if (value == null || value == DBNull.Value)
            return GetDefaultValue(targetType);

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return dataType switch
        {
            DataType.Timestamp or DataType.TimestampTz => ConvertToDateTime(value, underlyingType),
            DataType.Date => ConvertToDate(value, underlyingType),
            DataType.Time or DataType.TimeTz => ConvertToTime(value, underlyingType),
            DataType.Bytea => ConvertToBinary(value, underlyingType),
            DataType.Uuid => ConvertToGuid(value, underlyingType),
            DataType.Bool => ConvertToBoolean(value, underlyingType),
            DataType.Json or DataType.Jsonb => ConvertFromJson(value, underlyingType),
            DataType.SmallInt or DataType.Int or DataType.BigInt => ConvertToInteger(value, underlyingType),
            DataType.Real or DataType.DoublePrecision or DataType.Numeric => ConvertToDecimal(value, underlyingType),
            DataType.IntArray => ConvertToIntArray(value, underlyingType),
            DataType.BigIntArray => ConvertToBigIntArray(value, underlyingType),
            DataType.TextArray => ConvertToTextArray(value, underlyingType),
            DataType.NumericArray => ConvertToNumericArray(value, underlyingType),
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
                NpgsqlDateTime npdt => (DateTime)npdt,
                string s when DateTime.TryParse(s, out var dt) => dt,
                _ => DateTime.MinValue
            };
        }

        if (targetType == typeof(DateTimeOffset))
        {
            return value switch
            {
                DateTimeOffset dto => dto,
                DateTime dt => new DateTimeOffset(dt),
                NpgsqlDateTime npdt => new DateTimeOffset((DateTime)npdt),
                string s when DateTimeOffset.TryParse(s, out var dto) => dto,
                _ => DateTimeOffset.MinValue
            };
        }

        return GetDefaultValue(targetType);
    }

    private static object? ConvertToDate(object value, Type targetType)
    {
        if (targetType == typeof(DateOnly))
        {
            return value switch
            {
                DateOnly d => d,
                DateTime dt => DateOnly.FromDateTime(dt),
                NpgsqlDateTime npdt => DateOnly.FromDateTime((DateTime)npdt),
                string s when DateOnly.TryParse(s, out var d) => d,
                _ => DateOnly.MinValue
            };
        }

        if (targetType == typeof(DateTime))
        {
            return value switch
            {
                DateTime dt => dt,
                DateOnly d => d.ToDateTime(new TimeOnly()),
                NpgsqlDate npd => (DateTime)npd,
                string s when DateTime.TryParse(s, out var dt) => dt,
                _ => DateTime.MinValue
            };
        }

        return GetDefaultValue(targetType);
    }

    private static object? ConvertToTime(object value, Type targetType)
    {
        if (targetType == typeof(TimeOnly))
        {
            return value switch
            {
                TimeOnly t => t,
                TimeSpan ts => TimeOnly.FromTimeSpan(ts),
                NpgsqlTime npt => (TimeOnly)npt,
                string s when TimeOnly.TryParse(s, out var t) => t,
                _ => TimeOnly.MinValue
            };
        }

        if (targetType == typeof(TimeSpan))
        {
            return value switch
            {
                TimeSpan ts => ts,
                TimeOnly t => t.ToTimeSpan(),
                NpgsqlTime npt => (TimeSpan)npt,
                string s when TimeSpan.TryParse(s, out var ts) => ts,
                _ => TimeSpan.Zero
            };
        }

        return GetDefaultValue(targetType);
    }

    private static object? ConvertToBinary(object value, Type targetType)
    {
        if (targetType == typeof(byte[]))
        {
            return value switch
            {
                byte[] b => b,
                string s => Encoding.UTF8.GetBytes(s),
                _ => null
            };
        }

        return GetDefaultValue(targetType);
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

        return GetDefaultValue(targetType);
    }

    private static object? ConvertToBoolean(object value, Type targetType)
    {
        if (targetType == typeof(bool))
        {
            return value switch
            {
                bool b => b,
                int i => i != 0,
                byte b => b != 0,
                long l => l != 0,
                string s => s.Equals("true", StringComparison.OrdinalIgnoreCase),
                _ => Convert.ToBoolean(value)
            };
        }

        return GetDefaultValue(targetType);
    }

    private static object? ConvertFromJson(object value, Type targetType)
    {
        try
        {
            var json = value switch
            {
                string s => s,
                _ => value.ToString()
            };

            if (string.IsNullOrEmpty(json))
                return GetDefaultValue(targetType);

            return System.Text.Json.JsonSerializer.Deserialize(json, targetType);
        }
        catch
        {
            return GetDefaultValue(targetType);
        }
    }

    private static object? ConvertToInteger(object value, Type targetType)
    {
        if (targetType == typeof(int))
            return value switch
            {
                int i => i,
                long l => (int)l,
                string s when int.TryParse(s, out var i) => i,
                _ => 0
            };

        if (targetType == typeof(long))
            return value switch
            {
                long l => l,
                int i => (long)i,
                string s when long.TryParse(s, out var l) => l,
                _ => 0L
            };

        if (targetType == typeof(short))
            return value switch
            {
                short s => s,
                int i => (short)i,
                string s when short.TryParse(s, out var sh) => sh,
                _ => (short)0
            };

        return GetDefaultValue(targetType);
    }

    private static object? ConvertToDecimal(object value, Type targetType)
    {
        if (targetType == typeof(decimal))
            return value switch
            {
                decimal d => d,
                double d => (decimal)d,
                float f => (decimal)f,
                int i => i,
                long l => l,
                string s when decimal.TryParse(s, out var d) => d,
                _ => 0m
            };

        if (targetType == typeof(double))
            return value switch
            {
                double d => d,
                decimal d => (double)d,
                float f => (double)f,
                string s when double.TryParse(s, out var d) => d,
                _ => 0.0
            };

        if (targetType == typeof(float))
            return value switch
            {
                float f => f,
                double d => (float)d,
                decimal d => (float)d,
                string s when float.TryParse(s, out var f) => f,
                _ => 0f
            };

        return GetDefaultValue(targetType);
    }

    private static object? ConvertToIntArray(object value, Type targetType)
    {
        if (targetType == typeof(int[]))
        {
            return value switch
            {
                int[] arr => arr,
                List<int> list => list.ToArray(),
                _ => Array.Empty<int>()
            };
        }

        return GetDefaultValue(targetType);
    }

    private static object? ConvertToBigIntArray(object value, Type targetType)
    {
        if (targetType == typeof(long[]))
        {
            return value switch
            {
                long[] arr => arr,
                List<long> list => list.ToArray(),
                _ => Array.Empty<long>()
            };
        }

        return GetDefaultValue(targetType);
    }

    private static object? ConvertToTextArray(object value, Type targetType)
    {
        if (targetType == typeof(string[]))
        {
            return value switch
            {
                string[] arr => arr,
                List<string> list => list.ToArray(),
                _ => Array.Empty<string>()
            };
        }

        return GetDefaultValue(targetType);
    }

    private static object? ConvertToNumericArray(object value, Type targetType)
    {
        if (targetType == typeof(decimal[]))
        {
            return value switch
            {
                decimal[] arr => arr,
                List<decimal> list => list.ToArray(),
                _ => Array.Empty<decimal>()
            };
        }

        return GetDefaultValue(targetType);
    }

    private static object? ConvertToType(object value, Type targetType)
    {
        try
        {
            if (value.GetType() == targetType)
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
        if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            return Activator.CreateInstance(type);

        return null;
    }
}
