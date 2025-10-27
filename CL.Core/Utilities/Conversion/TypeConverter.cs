using System.ComponentModel;
using System.Globalization;

namespace CL.Core.Utilities.Conversion;

/// <summary>
/// Provides type conversion utilities
/// </summary>
public static class TypeConverter
{
    /// <summary>
    /// Safely converts object to int
    /// </summary>
    public static int ToInt(object? value, int defaultValue = 0)
    {
        if (value == null)
            return defaultValue;

        try
        {
            return Convert.ToInt32(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Safely converts object to long
    /// </summary>
    public static long ToLong(object? value, long defaultValue = 0)
    {
        if (value == null)
            return defaultValue;

        try
        {
            return Convert.ToInt64(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Safely converts object to double
    /// </summary>
    public static double ToDouble(object? value, double defaultValue = 0.0)
    {
        if (value == null)
            return defaultValue;

        try
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Safely converts object to decimal
    /// </summary>
    public static decimal ToDecimal(object? value, decimal defaultValue = 0)
    {
        if (value == null)
            return defaultValue;

        try
        {
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Safely converts object to bool
    /// </summary>
    public static bool ToBool(object? value, bool defaultValue = false)
    {
        if (value == null)
            return defaultValue;

        try
        {
            if (value is string str)
            {
                return str.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                       str.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                       str.Equals("1", StringComparison.OrdinalIgnoreCase);
            }

            return Convert.ToBoolean(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Safely converts object to string
    /// </summary>
    public static string ToString(object? value, string defaultValue = "")
    {
        return value?.ToString() ?? defaultValue;
    }

    /// <summary>
    /// Safely converts object to DateTime
    /// </summary>
    public static DateTime ToDateTime(object? value, DateTime? defaultValue = null)
    {
        if (value == null)
            return defaultValue ?? DateTime.MinValue;

        try
        {
            return Convert.ToDateTime(value);
        }
        catch
        {
            return defaultValue ?? DateTime.MinValue;
        }
    }

    /// <summary>
    /// Converts object to dictionary
    /// </summary>
    public static Dictionary<string, object?> ToDictionary(object obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        var dictionary = new Dictionary<string, object?>();

        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(obj))
        {
            dictionary[property.Name] = property.GetValue(obj);
        }

        return dictionary;
    }

    /// <summary>
    /// Converts object to byte array
    /// </summary>
    public static byte[] ToByteArray(object obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        if (obj is byte[] bytes)
            return bytes;

        if (obj is string str)
            return System.Text.Encoding.UTF8.GetBytes(str);

        // For other types, serialize to JSON then to bytes
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Tries to convert value to specified type
    /// </summary>
    public static bool TryConvert<T>(object? value, out T? result)
    {
        result = default;

        if (value == null)
            return false;

        try
        {
            result = (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
