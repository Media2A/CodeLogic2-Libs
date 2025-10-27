namespace CL.SystemStats.Models;

/// <summary>
/// Represents statistics for a system process
/// </summary>
public record ProcessStats
{
    /// <summary>
    /// Gets the process ID
    /// </summary>
    public required int ProcessId { get; init; }

    /// <summary>
    /// Gets the process name
    /// </summary>
    public required string ProcessName { get; init; }

    /// <summary>
    /// Gets the process priority
    /// </summary>
    public string? Priority { get; init; }

    /// <summary>
    /// Gets the CPU usage percentage
    /// </summary>
    public required double CpuUsagePercent { get; init; }

    /// <summary>
    /// Gets the memory usage in bytes
    /// </summary>
    public required long MemoryBytes { get; init; }

    /// <summary>
    /// Gets the virtual memory in bytes
    /// </summary>
    public long? VirtualMemoryBytes { get; init; }

    /// <summary>
    /// Gets the number of threads
    /// </summary>
    public int? ThreadCount { get; init; }

    /// <summary>
    /// Gets the handle/file descriptor count
    /// </summary>
    public int? HandleCount { get; init; }

    /// <summary>
    /// Gets the process state (Windows)
    /// </summary>
    public string? ProcessState { get; init; }

    /// <summary>
    /// Gets the process start time
    /// </summary>
    public DateTime? StartTime { get; init; }

    /// <summary>
    /// Gets when the stats were captured
    /// </summary>
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the memory usage in MB
    /// </summary>
    public double GetMemoryMb() => MemoryBytes / (1024.0 * 1024.0);

    /// <summary>
    /// Gets the uptime of the process
    /// </summary>
    public TimeSpan? GetUptime() =>
        StartTime.HasValue ? DateTime.UtcNow - StartTime.Value : null;
}

/// <summary>
/// Represents a complete system snapshot
/// </summary>
public record SystemSnapshot
{
    /// <summary>
    /// Gets the CPU information
    /// </summary>
    public required CpuInfo CpuInfo { get; init; }

    /// <summary>
    /// Gets the CPU statistics
    /// </summary>
    public required CpuStats CpuStats { get; init; }

    /// <summary>
    /// Gets the memory information
    /// </summary>
    public required MemoryInfo MemoryInfo { get; init; }

    /// <summary>
    /// Gets the memory statistics
    /// </summary>
    public required MemoryStats MemoryStats { get; init; }

    /// <summary>
    /// Gets the system uptime
    /// </summary>
    public required TimeSpan SystemUptime { get; init; }

    /// <summary>
    /// Gets the process count
    /// </summary>
    public int ProcessCount { get; init; }

    /// <summary>
    /// Gets when the snapshot was taken
    /// </summary>
    public DateTime SnapshotTime { get; init; } = DateTime.UtcNow;
}
