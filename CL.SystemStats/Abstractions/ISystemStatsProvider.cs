namespace CL.SystemStats.Abstractions;

using Models;

/// <summary>
/// Defines the contract for platform-specific system stats providers
/// </summary>
public interface ISystemStatsProvider
{
    /// <summary>
    /// Gets the CPU information for the current system
    /// </summary>
    Task<SystemStatsResult<CpuInfo>> GetCpuInfoAsync();

    /// <summary>
    /// Gets the current CPU statistics
    /// </summary>
    Task<SystemStatsResult<CpuStats>> GetCpuStatsAsync();

    /// <summary>
    /// Gets the memory information for the current system
    /// </summary>
    Task<SystemStatsResult<MemoryInfo>> GetMemoryInfoAsync();

    /// <summary>
    /// Gets the current memory statistics
    /// </summary>
    Task<SystemStatsResult<MemoryStats>> GetMemoryStatsAsync();

    /// <summary>
    /// Gets the current system uptime
    /// </summary>
    Task<SystemStatsResult<TimeSpan>> GetSystemUptimeAsync();

    /// <summary>
    /// Gets statistics for a specific process
    /// </summary>
    /// <param name="processId">The process ID to get stats for</param>
    Task<SystemStatsResult<ProcessStats>> GetProcessStatsAsync(int processId);

    /// <summary>
    /// Gets all running processes
    /// </summary>
    Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetAllProcessesAsync();

    /// <summary>
    /// Gets the top N processes by CPU usage
    /// </summary>
    /// <param name="topCount">Number of top processes to return</param>
    Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetTopProcessesByCpuAsync(int topCount);

    /// <summary>
    /// Gets the top N processes by memory usage
    /// </summary>
    /// <param name="topCount">Number of top processes to return</param>
    Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetTopProcessesByMemoryAsync(int topCount);

    /// <summary>
    /// Gets a complete system snapshot
    /// </summary>
    Task<SystemStatsResult<SystemSnapshot>> GetSystemSnapshotAsync();

    /// <summary>
    /// Initializes the provider
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Disposes the provider resources
    /// </summary>
    ValueTask DisposeAsync();
}
