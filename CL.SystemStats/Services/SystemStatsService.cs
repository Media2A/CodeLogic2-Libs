namespace CL.SystemStats.Services;

using System.Collections.Concurrent;
using Abstractions;
using Models;
using Providers;

/// <summary>
/// Main service for retrieving system statistics with caching support
/// </summary>
public class SystemStatsService : IAsyncDisposable
{
    private readonly SystemStatsConfiguration _config;
    private readonly IPlatformDetector _platformDetector;
    private ISystemStatsProvider? _provider;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = [];
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the SystemStatsService class
    /// </summary>
    public SystemStatsService(SystemStatsConfiguration? config = null)
    {
        _config = config ?? new SystemStatsConfiguration();
        _platformDetector = new PlatformDetector();
    }

    /// <summary>
    /// Initializes the service and detects the appropriate platform provider
    /// </summary>
    public async Task<SystemStatsResult> InitializeAsync()
    {
        try
        {
            if (!_platformDetector.IsSupported)
                return SystemStatsResult.Failure(
                    SystemStatsError.UnsupportedPlatform,
                    $"Platform not supported: {_platformDetector.PlatformDescription}");

            _provider = _platformDetector.CurrentPlatform switch
            {
                PlatformType.Windows => new WindowsSystemStatsProvider(_config),
                PlatformType.Linux => new LinuxSystemStatsProvider(_config),
                _ => null
            };

            if (_provider == null)
                return SystemStatsResult.Failure(
                    SystemStatsError.UnsupportedPlatform,
                    "Could not create provider for platform");

            await _provider.InitializeAsync();
            _initialized = true;
            return SystemStatsResult.Success();
        }
        catch (Exception ex)
        {
            return SystemStatsResult.Failure(
                SystemStatsError.NotInitialized,
                $"Failed to initialize service: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the CPU information
    /// </summary>
    public async Task<SystemStatsResult<CpuInfo>> GetCpuInfoAsync()
    {
        if (!_initialized || _provider == null)
            return SystemStatsResult<CpuInfo>.Failure(
                SystemStatsError.NotInitialized,
                "Service not initialized");

        const string cacheKey = "cpu_info";

        if (_config.EnableCaching && GetCachedValue<CpuInfo>(cacheKey, out var cached))
            return cached;

        var result = await _provider.GetCpuInfoAsync();

        if (result.IsSuccess && _config.EnableCaching)
            SetCachedValue(cacheKey, result.Value!);

        return result;
    }

    /// <summary>
    /// Gets the current CPU statistics
    /// </summary>
    public async Task<SystemStatsResult<CpuStats>> GetCpuStatsAsync()
    {
        if (!_initialized || _provider == null)
            return SystemStatsResult<CpuStats>.Failure(
                SystemStatsError.NotInitialized,
                "Service not initialized");

        const string cacheKey = "cpu_stats";

        if (_config.EnableCaching && GetCachedValue<CpuStats>(cacheKey, out var cached))
            return cached;

        var result = await _provider.GetCpuStatsAsync();

        if (result.IsSuccess && _config.EnableCaching)
            SetCachedValue(cacheKey, result.Value!);

        return result;
    }

    /// <summary>
    /// Gets the memory information
    /// </summary>
    public async Task<SystemStatsResult<MemoryInfo>> GetMemoryInfoAsync()
    {
        if (!_initialized || _provider == null)
            return SystemStatsResult<MemoryInfo>.Failure(
                SystemStatsError.NotInitialized,
                "Service not initialized");

        const string cacheKey = "memory_info";

        if (_config.EnableCaching && GetCachedValue<MemoryInfo>(cacheKey, out var cached))
            return cached;

        var result = await _provider.GetMemoryInfoAsync();

        if (result.IsSuccess && _config.EnableCaching)
            SetCachedValue(cacheKey, result.Value!);

        return result;
    }

    /// <summary>
    /// Gets the current memory statistics
    /// </summary>
    public async Task<SystemStatsResult<MemoryStats>> GetMemoryStatsAsync()
    {
        if (!_initialized || _provider == null)
            return SystemStatsResult<MemoryStats>.Failure(
                SystemStatsError.NotInitialized,
                "Service not initialized");

        const string cacheKey = "memory_stats";

        if (_config.EnableCaching && GetCachedValue<MemoryStats>(cacheKey, out var cached))
            return cached;

        var result = await _provider.GetMemoryStatsAsync();

        if (result.IsSuccess && _config.EnableCaching)
            SetCachedValue(cacheKey, result.Value!);

        return result;
    }

    /// <summary>
    /// Gets the system uptime
    /// </summary>
    public async Task<SystemStatsResult<TimeSpan>> GetSystemUptimeAsync()
    {
        if (!_initialized || _provider == null)
            return SystemStatsResult<TimeSpan>.Failure(
                SystemStatsError.NotInitialized,
                "Service not initialized");

        return await _provider.GetSystemUptimeAsync();
    }

    /// <summary>
    /// Gets statistics for a specific process
    /// </summary>
    public async Task<SystemStatsResult<ProcessStats>> GetProcessStatsAsync(int processId)
    {
        if (!_initialized || _provider == null)
            return SystemStatsResult<ProcessStats>.Failure(
                SystemStatsError.NotInitialized,
                "Service not initialized");

        return await _provider.GetProcessStatsAsync(processId);
    }

    /// <summary>
    /// Gets all running processes
    /// </summary>
    public async Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetAllProcessesAsync()
    {
        if (!_initialized || _provider == null)
            return SystemStatsResult<IReadOnlyList<ProcessStats>>.Failure(
                SystemStatsError.NotInitialized,
                "Service not initialized");

        return await _provider.GetAllProcessesAsync();
    }

    /// <summary>
    /// Gets the top N processes by CPU usage
    /// </summary>
    public async Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetTopProcessesByCpuAsync(int topCount = 10)
    {
        if (!_initialized || _provider == null)
            return SystemStatsResult<IReadOnlyList<ProcessStats>>.Failure(
                SystemStatsError.NotInitialized,
                "Service not initialized");

        return await _provider.GetTopProcessesByCpuAsync(Math.Max(1, topCount));
    }

    /// <summary>
    /// Gets the top N processes by memory usage
    /// </summary>
    public async Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetTopProcessesByMemoryAsync(int topCount = 10)
    {
        if (!_initialized || _provider == null)
            return SystemStatsResult<IReadOnlyList<ProcessStats>>.Failure(
                SystemStatsError.NotInitialized,
                "Service not initialized");

        return await _provider.GetTopProcessesByMemoryAsync(Math.Max(1, topCount));
    }

    /// <summary>
    /// Gets a complete system snapshot
    /// </summary>
    public async Task<SystemStatsResult<SystemSnapshot>> GetSystemSnapshotAsync()
    {
        if (!_initialized || _provider == null)
            return SystemStatsResult<SystemSnapshot>.Failure(
                SystemStatsError.NotInitialized,
                "Service not initialized");

        const string cacheKey = "system_snapshot";

        if (_config.EnableCaching && GetCachedValue<SystemSnapshot>(cacheKey, out var cached))
            return cached;

        var result = await _provider.GetSystemSnapshotAsync();

        if (result.IsSuccess && _config.EnableCaching)
            SetCachedValue(cacheKey, result.Value!);

        return result;
    }

    /// <summary>
    /// Clears all cached data
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets the detected platform information
    /// </summary>
    public string GetPlatformInfo()
    {
        return _platformDetector.PlatformDescription;
    }

    /// <summary>
    /// Gets whether the service is initialized
    /// </summary>
    public bool IsInitialized => _initialized;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_provider != null)
            await _provider.DisposeAsync();

        _cache.Clear();
        _initialized = false;
    }

    /// <summary>
    /// Gets a value from cache if it exists and is not expired
    /// </summary>
    private bool GetCachedValue<T>(string key, out SystemStatsResult<T> result)
    {
        result = null!;

        if (!_cache.TryGetValue(key, out var entry))
            return false;

        if (DateTime.UtcNow - entry.CreatedAt > TimeSpan.FromSeconds(_config.CacheDurationSeconds))
        {
            _cache.TryRemove(key, out _);
            return false;
        }

        if (entry.Value is SystemStatsResult<T> typedResult)
        {
            result = typedResult;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets a value in cache
    /// </summary>
    private void SetCachedValue<T>(string key, T value)
    {
        var result = SystemStatsResult<T>.Success(value);
        _cache[key] = new CacheEntry
        {
            Value = result,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Internal cache entry class
    /// </summary>
    private class CacheEntry
    {
        public required object Value { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
}
