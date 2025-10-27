namespace CL.SystemStats;

using CodeLogic.Abstractions;
using CodeLogic.Models;
using Models;
using Services;

/// <summary>
/// CL.SystemStats library for retrieving cross-platform system statistics
/// </summary>
public class SystemStatsLibrary : ILibrary
{
    private SystemStatsService? _systemStatsService;
    private ILogger? _logger;
    private SystemStatsConfiguration? _config;
    private bool _initialized;

    /// <summary>
    /// Gets the library manifest
    /// </summary>
    public ILibraryManifest Manifest { get; } = new SystemStatsManifest();

    /// <summary>
    /// Called when the library is loaded
    /// </summary>
    public Task OnLoadAsync(LibraryContext context)
    {
        _logger = context.Logger;
        _logger.Info($"Loading {Manifest.Name} v{Manifest.Version}");

        // Get configuration or use defaults
        _config = context.Configuration.TryGetValue("SystemStats", out var statsObj) && statsObj is SystemStatsConfiguration statsCfg
            ? statsCfg
            : new SystemStatsConfiguration();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called to initialize the library after dependencies are loaded
    /// </summary>
    public async Task OnInitializeAsync()
    {
        if (_logger == null || _config == null)
            throw new InvalidOperationException("Library not loaded");

        _logger.Info($"Initializing {Manifest.Name}");

        _systemStatsService = new SystemStatsService(_config);
        var initResult = await _systemStatsService.InitializeAsync();

        if (!initResult.IsSuccess)
            throw new InvalidOperationException($"Failed to initialize system stats service: {initResult.ErrorMessage}");

        _initialized = true;
        _logger.Info($"{Manifest.Name} initialized successfully - Platform: {_systemStatsService.GetPlatformInfo()}");
    }

    /// <summary>
    /// Called when the library is being unloaded
    /// </summary>
    public async Task OnUnloadAsync()
    {
        _logger?.Info($"Unloading {Manifest.Name}");

        if (_systemStatsService != null)
            await _systemStatsService.DisposeAsync();

        _systemStatsService = null;
        _initialized = false;

        _logger?.Info($"{Manifest.Name} unloaded successfully");
    }

    /// <summary>
    /// Performs a health check on the library
    /// </summary>
    public Task<HealthCheckResult> HealthCheckAsync()
    {
        if (!_initialized || _systemStatsService == null || !_systemStatsService.IsInitialized)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Library not initialized",
                new InvalidOperationException("Library is not properly initialized")));
        }

        try
        {
            // Service is ready
            return Task.FromResult(HealthCheckResult.Healthy($"{Manifest.Name} is operational"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Health check failed",
                ex));
        }
    }

    /// <summary>
    /// Gets the system statistics service
    /// </summary>
    public SystemStatsService GetSystemStatsService()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return _systemStatsService;
    }

    /// <summary>
    /// Gets CPU information
    /// </summary>
    public async Task<SystemStatsResult<CpuInfo>> GetCpuInfoAsync()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetCpuInfoAsync();
    }

    /// <summary>
    /// Gets current CPU statistics
    /// </summary>
    public async Task<SystemStatsResult<CpuStats>> GetCpuStatsAsync()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetCpuStatsAsync();
    }

    /// <summary>
    /// Gets memory information
    /// </summary>
    public async Task<SystemStatsResult<MemoryInfo>> GetMemoryInfoAsync()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetMemoryInfoAsync();
    }

    /// <summary>
    /// Gets current memory statistics
    /// </summary>
    public async Task<SystemStatsResult<MemoryStats>> GetMemoryStatsAsync()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetMemoryStatsAsync();
    }

    /// <summary>
    /// Gets system uptime
    /// </summary>
    public async Task<SystemStatsResult<TimeSpan>> GetSystemUptimeAsync()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetSystemUptimeAsync();
    }

    /// <summary>
    /// Gets statistics for a specific process
    /// </summary>
    public async Task<SystemStatsResult<ProcessStats>> GetProcessStatsAsync(int processId)
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetProcessStatsAsync(processId);
    }

    /// <summary>
    /// Gets all running processes
    /// </summary>
    public async Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetAllProcessesAsync()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetAllProcessesAsync();
    }

    /// <summary>
    /// Gets top processes by CPU usage
    /// </summary>
    public async Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetTopProcessesByCpuAsync(int topCount = 10)
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetTopProcessesByCpuAsync(topCount);
    }

    /// <summary>
    /// Gets top processes by memory usage
    /// </summary>
    public async Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetTopProcessesByMemoryAsync(int topCount = 10)
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetTopProcessesByMemoryAsync(topCount);
    }

    /// <summary>
    /// Gets a complete system snapshot
    /// </summary>
    public async Task<SystemStatsResult<SystemSnapshot>> GetSystemSnapshotAsync()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetSystemSnapshotAsync();
    }
}

/// <summary>
/// Library manifest for CL.SystemStats
/// </summary>
internal class SystemStatsManifest : ILibraryManifest
{
    /// <summary>
    /// Gets the library ID
    /// </summary>
    public string Id => "cl.systemstats";

    /// <summary>
    /// Gets the library name
    /// </summary>
    public string Name => "CL.SystemStats";

    /// <summary>
    /// Gets the library version
    /// </summary>
    public string Version => "2.0.0";

    /// <summary>
    /// Gets the library author
    /// </summary>
    public string Author => "Media2A.com";

    /// <summary>
    /// Gets the library description
    /// </summary>
    public string Description => "Cross-platform system statistics library supporting Windows and Linux";

    /// <summary>
    /// Gets library dependencies
    /// </summary>
    public IReadOnlyList<LibraryDependency> Dependencies { get; } = new List<LibraryDependency>();

    /// <summary>
    /// Gets library tags
    /// </summary>
    public IReadOnlyList<string> Tags { get; } = new List<string>
    {
        "system", "stats", "monitoring", "cpu", "memory", "processes", "cross-platform", "windows", "linux"
    };
}
