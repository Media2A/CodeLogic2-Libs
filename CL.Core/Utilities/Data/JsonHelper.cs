using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CL.Core.Utilities.Data;

/// <summary>
/// Provides JSON serialization and deserialization utilities
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerSettings _defaultSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    /// <summary>
    /// Validates if a string is valid JSON
    /// </summary>
    public static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            JToken.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Serializes an object to JSON string
    /// </summary>
    public static string Serialize(object obj, Formatting formatting = Formatting.Indented)
    {
        return JsonConvert.SerializeObject(obj, formatting, _defaultSettings);
    }

    /// <summary>
    /// Serializes an object to JSON string with custom settings
    /// </summary>
    public static string Serialize(object obj, JsonSerializerSettings settings)
    {
        return JsonConvert.SerializeObject(obj, settings);
    }

    /// <summary>
    /// Deserializes JSON string to object
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonConvert.DeserializeObject<T>(json, _defaultSettings);
    }

    /// <summary>
    /// Deserializes JSON string to object with custom settings
    /// </summary>
    public static T? Deserialize<T>(string json, JsonSerializerSettings settings)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonConvert.DeserializeObject<T>(json, settings);
    }

    /// <summary>
    /// Deserializes JSON string to dynamic object
    /// </summary>
    public static dynamic? DeserializeDynamic(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonConvert.DeserializeObject(json);
    }

    /// <summary>
    /// Deserializes JSON string to dictionary
    /// </summary>
    public static Dictionary<string, object>? DeserializeToDictionary(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
    }

    /// <summary>
    /// Gets a value from JSON by key
    /// </summary>
    public static string? GetValue(string json, string key)
    {
        var dict = DeserializeToDictionary(json);
        if (dict != null && dict.TryGetValue(key, out var value))
        {
            return value?.ToString();
        }
        return null;
    }

    /// <summary>
    /// Gets a nested value from JSON
    /// </summary>
    public static string? GetNestedValue(string json, string root, string path)
    {
        var dict = DeserializeToDictionary(json);

        if (dict == null || !dict.TryGetValue(root, out var rootValue))
            return null;

        if (rootValue is not Dictionary<string, object> currentDict)
            return null;

        string[] keys = path.Split('.');

        foreach (string key in keys)
        {
            if (currentDict.TryGetValue(key, out var value))
            {
                if (value is Dictionary<string, object> nestedDict)
                {
                    currentDict = nestedDict;
                }
                else
                {
                    return value?.ToString();
                }
            }
            else
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to deserialize JSON, returning a result indicating success or failure
    /// </summary>
    public static bool TryDeserialize<T>(string json, out T? result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            result = Deserialize<T>(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Clones an object by serializing and deserializing it
    /// </summary>
    public static T? Clone<T>(T obj)
    {
        if (obj == null)
            return default;

        var json = Serialize(obj);
        return Deserialize<T>(json);
    }

    /// <summary>
    /// Merges two JSON objects
    /// </summary>
    public static string Merge(string json1, string json2)
    {
        var obj1 = JObject.Parse(json1);
        var obj2 = JObject.Parse(json2);

        obj1.Merge(obj2, new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Union
        });

        return obj1.ToString();
    }
}
