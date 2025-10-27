namespace CL.SystemStats.Models;

/// <summary>
/// Represents memory configuration information
/// </summary>
public record MemoryInfo
{
    /// <summary>
    /// Gets the total system memory in bytes
    /// </summary>
    public required long TotalBytes { get; init; }

    /// <summary>
    /// Gets the memory manufacturer (if available)
    /// </summary>
    public string? Manufacturer { get; init; }

    /// <summary>
    /// Gets the memory type (e.g., DDR4, DDR5)
    /// </summary>
    public string? MemoryType { get; init; }

    /// <summary>
    /// Gets the memory speed in MHz (if available)
    /// </summary>
    public int? SpeedMhz { get; init; }

    /// <summary>
    /// Gets when the info was retrieved
    /// </summary>
    public DateTime RetrievedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets total memory in GB
    /// </summary>
    public double GetTotalGb() => TotalBytes / (1024.0 * 1024.0 * 1024.0);

    /// <summary>
    /// Gets total memory in MB
    /// </summary>
    public double GetTotalMb() => TotalBytes / (1024.0 * 1024.0);
}

/// <summary>
/// Represents memory usage statistics
/// </summary>
public record MemoryStats
{
    /// <summary>
    /// Gets the total system memory in bytes
    /// </summary>
    public required long TotalBytes { get; init; }

    /// <summary>
    /// Gets the used memory in bytes
    /// </summary>
    public required long UsedBytes { get; init; }

    /// <summary>
    /// Gets the available memory in bytes
    /// </summary>
    public required long AvailableBytes { get; init; }

    /// <summary>
    /// Gets the free memory in bytes
    /// </summary>
    public required long FreeBytes { get; init; }

    /// <summary>
    /// Gets the memory usage percentage (0-100)
    /// </summary>
    public required double UsagePercent { get; init; }

    /// <summary>
    /// Gets the cached memory in bytes (Linux)
    /// </summary>
    public long? CachedBytes { get; init; }

    /// <summary>
    /// Gets the buffered memory in bytes (Linux)
    /// </summary>
    public long? BufferedBytes { get; init; }

    /// <summary>
    /// Gets when the stats were captured
    /// </summary>
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets total memory in GB
    /// </summary>
    public double GetTotalGb() => TotalBytes / (1024.0 * 1024.0 * 1024.0);

    /// <summary>
    /// Gets used memory in GB
    /// </summary>
    public double GetUsedGb() => UsedBytes / (1024.0 * 1024.0 * 1024.0);

    /// <summary>
    /// Gets available memory in GB
    /// </summary>
    public double GetAvailableGb() => AvailableBytes / (1024.0 * 1024.0 * 1024.0);

    /// <summary>
    /// Gets free memory in GB
    /// </summary>
    public double GetFreeGb() => FreeBytes / (1024.0 * 1024.0 * 1024.0);
}
