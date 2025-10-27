namespace CL.SystemStats.Abstractions;

/// <summary>
/// Defines the contract for platform detection
/// </summary>
public interface IPlatformDetector
{
    /// <summary>
    /// Gets the current platform type
    /// </summary>
    PlatformType CurrentPlatform { get; }

    /// <summary>
    /// Determines if the current platform is supported
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Gets a human-readable platform description
    /// </summary>
    string PlatformDescription { get; }

    /// <summary>
    /// Gets the operating system version
    /// </summary>
    string OsVersion { get; }
}

/// <summary>
/// Enumeration of supported platforms
/// </summary>
public enum PlatformType
{
    /// <summary>
    /// Microsoft Windows
    /// </summary>
    Windows,

    /// <summary>
    /// Linux operating system
    /// </summary>
    Linux,

    /// <summary>
    /// Apple macOS
    /// </summary>
    MacOS,

    /// <summary>
    /// Unknown or unsupported platform
    /// </summary>
    Unknown
}
