using System.Text;
using K4os.Compression.LZ4.Streams;
using Newtonsoft.Json;

namespace CL.Core.Utilities.Compression;

/// <summary>
/// Provides LZ4 compression and decompression utilities
/// </summary>
public static class CompressionHelper
{
    /// <summary>
    /// Compresses data using LZ4 algorithm
    /// </summary>
    public static byte[] Compress<T>(T data, K4os.Compression.LZ4.LZ4Level level = K4os.Compression.LZ4.LZ4Level.L00_FAST)
    {
        byte[] bytes = ConvertToBytes(data);

        using var outputStream = new MemoryStream();
        using (var lz4Stream = LZ4Stream.Encode(outputStream, level))
        {
            lz4Stream.Write(bytes, 0, bytes.Length);
        }

        return outputStream.ToArray();
    }

    /// <summary>
    /// Decompresses LZ4-compressed data
    /// </summary>
    public static T? Decompress<T>(byte[] compressedData)
    {
        if (compressedData == null || compressedData.Length == 0)
            return default;

        using var inputStream = new MemoryStream(compressedData);
        using var lz4Stream = LZ4Stream.Decode(inputStream);
        using var outputStream = new MemoryStream();

        lz4Stream.CopyTo(outputStream);
        byte[] decompressedBytes = outputStream.ToArray();

        return ConvertFromBytes<T>(decompressedBytes);
    }

    /// <summary>
    /// Compresses a string using LZ4
    /// </summary>
    public static byte[] CompressString(string text, K4os.Compression.LZ4.LZ4Level level = K4os.Compression.LZ4.LZ4Level.L00_FAST)
    {
        return Compress(text, level);
    }

    /// <summary>
    /// Decompresses a string from LZ4-compressed data
    /// </summary>
    public static string? DecompressString(byte[] compressedData)
    {
        return Decompress<string>(compressedData);
    }

    /// <summary>
    /// Compresses a stream using LZ4
    /// </summary>
    public static async Task CompressStreamAsync(Stream input, Stream output, K4os.Compression.LZ4.LZ4Level level = K4os.Compression.LZ4.LZ4Level.L00_FAST)
    {
        using var lz4Stream = LZ4Stream.Encode(output, level, leaveOpen: true);
        await input.CopyToAsync(lz4Stream);
    }

    /// <summary>
    /// Decompresses a stream from LZ4-compressed data
    /// </summary>
    public static async Task DecompressStreamAsync(Stream input, Stream output)
    {
        using var lz4Stream = LZ4Stream.Decode(input, leaveOpen: true);
        await lz4Stream.CopyToAsync(output);
    }

    /// <summary>
    /// Calculates compression ratio
    /// </summary>
    public static double CalculateCompressionRatio(int originalSize, int compressedSize)
    {
        if (originalSize == 0)
            return 0;

        return (double)compressedSize / originalSize;
    }

    private static byte[] ConvertToBytes<T>(T obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        return obj switch
        {
            string str => Encoding.UTF8.GetBytes(str),
            byte[] bytes => bytes,
            _ => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj))
        };
    }

    private static T? ConvertFromBytes<T>(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return default;

        var targetType = typeof(T);

        if (targetType == typeof(string))
        {
            return (T)(object)Encoding.UTF8.GetString(bytes);
        }

        if (targetType == typeof(byte[]))
        {
            return (T)(object)bytes;
        }

        // Try to deserialize as JSON for other types
        string json = Encoding.UTF8.GetString(bytes);
        return JsonConvert.DeserializeObject<T>(json);
    }
}
