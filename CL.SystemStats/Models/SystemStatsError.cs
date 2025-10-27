namespace CL.SystemStats.Models;

/// <summary>
/// Error types for system stats operations
/// </summary>
public enum SystemStatsError
{
    /// <summary>
    /// No error
    /// </summary>
    None,

    /// <summary>
    /// Platform not supported
    /// </summary>
    UnsupportedPlatform,

    /// <summary>
    /// Access denied (permission issue)
    /// </summary>
    AccessDenied,

    /// <summary>
    /// CPU information unavailable
    /// </summary>
    CpuInfoUnavailable,

    /// <summary>
    /// Memory information unavailable
    /// </summary>
    MemoryInfoUnavailable,

    /// <summary>
    /// Process statistics unavailable
    /// </summary>
    ProcessStatsUnavailable,

    /// <summary>
    /// File not found or /proc entry missing
    /// </summary>
    FileNotFound,

    /// <summary>
    /// Failed to parse system data
    /// </summary>
    ParseError,

    /// <summary>
    /// WMI query failed (Windows)
    /// </summary>
    WmiQueryFailed,

    /// <summary>
    /// /proc filesystem error (Linux)
    /// </summary>
    ProcFilesystemError,

    /// <summary>
    /// Service not initialized
    /// </summary>
    NotInitialized,

    /// <summary>
    /// Unknown error occurred
    /// </summary>
    Unknown
}
