namespace CL.SystemStats.Services;

using System.Runtime.InteropServices;
using Abstractions;

/// <summary>
/// Detects the current platform and provides platform information
/// </summary>
internal class PlatformDetector : IPlatformDetector
{
    private readonly PlatformType _currentPlatform;

    /// <summary>
    /// Initializes a new instance of the PlatformDetector class
    /// </summary>
    public PlatformDetector()
    {
        _currentPlatform = DetectPlatform();
    }

    /// <inheritdoc />
    public PlatformType CurrentPlatform => _currentPlatform;

    /// <inheritdoc />
    public bool IsSupported => _currentPlatform is PlatformType.Windows or PlatformType.Linux;

    /// <inheritdoc />
    public string PlatformDescription => _currentPlatform switch
    {
        PlatformType.Windows => $"Windows ({RuntimeInformation.OSDescription})",
        PlatformType.Linux => $"Linux ({RuntimeInformation.OSDescription})",
        PlatformType.MacOS => $"macOS ({RuntimeInformation.OSDescription})",
        _ => $"Unknown ({RuntimeInformation.OSDescription})"
    };

    /// <inheritdoc />
    public string OsVersion => RuntimeInformation.OSDescription;

    /// <summary>
    /// Detects the current platform type
    /// </summary>
    private static PlatformType DetectPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return PlatformType.Windows;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return PlatformType.Linux;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return PlatformType.MacOS;

        return PlatformType.Unknown;
    }
}
