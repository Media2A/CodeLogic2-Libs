namespace CL.SystemStats.Models;

/// <summary>
/// Configuration for the SystemStats library
/// </summary>
public class SystemStatsConfiguration
{
    /// <summary>
    /// Gets or sets whether to enable caching
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache duration in seconds
    /// </summary>
    public int CacheDurationSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the CPU sampling interval in milliseconds
    /// </summary>
    public int CpuSamplingIntervalMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets the number of CPU samples to average
    /// </summary>
    public int CpuSamplesForAverage { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether to enable CPU temperature monitoring
    /// </summary>
    public bool EnableTemperatureMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to monitor individual process statistics
    /// </summary>
    public bool EnableProcessMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of top processes to track
    /// </summary>
    public int MaxTopProcesses { get; set; } = 10;
}
