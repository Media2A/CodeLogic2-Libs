using CodeLogic.Abstractions;
using CL.GitHelper.Models;
using System.Collections.Concurrent;

namespace CL.GitHelper.Services;

/// <summary>
/// Manages multiple Git repositories with caching and lifecycle management
/// </summary>
public class GitManager : IDisposable
{
    private readonly GitHelperConfiguration _configuration;
    private readonly ILogger? _logger;
    private readonly ConcurrentDictionary<string, CachedRepository> _repositoryCache;
    private readonly SemaphoreSlim _cacheLock;
    private readonly Timer? _cleanupTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of GitManager
    /// </summary>
    /// <param name="configuration">Git configuration</param>
    /// <param name="logger">Optional logger instance</param>
    public GitManager(GitHelperConfiguration configuration, ILogger? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger;
        _repositoryCache = new ConcurrentDictionary<string, CachedRepository>();
        _cacheLock = new SemaphoreSlim(1, 1);

        // Start cleanup timer if caching is enabled
        if (_configuration.EnableRepositoryCaching && _configuration.CacheTimeoutMinutes > 0)
        {
            _cleanupTimer = new Timer(
                CleanupExpiredCaches,
                null,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1)
            );

            _logger?.LogDebug("GitManager",
                $"Cache cleanup timer started (interval: 1 minute, timeout: {_configuration.CacheTimeoutMinutes} minutes)");
        }

        _logger?.LogInfo("GitManager",
            $"Initialized with {_configuration.Repositories.Count} repository configuration(s)");
    }

    #region Repository Access

    /// <summary>
    /// Gets or creates a Git repository instance by ID
    /// </summary>
    /// <param name="repositoryId">Repository configuration ID</param>
    /// <returns>GitRepository instance</returns>
    /// <exception cref="ArgumentException">If repository ID not found in configuration</exception>
    public async Task<GitRepository> GetRepositoryAsync(string repositoryId = "Default")
    {
        // Get configuration
        var config = _configuration.GetRepository(repositoryId);
        if (config == null)
        {
            throw new ArgumentException($"Repository configuration not found: {repositoryId}", nameof(repositoryId));
        }

        // Check if caching is enabled
        if (!_configuration.EnableRepositoryCaching)
        {
            _logger?.LogDebug("GitManager", $"Creating new repository instance (caching disabled): {repositoryId}");
            return new GitRepository(config, _logger);
        }

        // Try to get from cache
        if (_repositoryCache.TryGetValue(repositoryId, out var cached))
        {
            // Check if cache is still valid
            if (IsValidCache(cached))
            {
                cached.LastAccessed = DateTime.UtcNow;
                _logger?.LogDebug("GitManager", $"Retrieved repository from cache: {repositoryId}");
                return cached.Repository;
            }
            else
            {
                // Cache expired, remove it
                _logger?.LogDebug("GitManager", $"Cache expired for repository: {repositoryId}");
                await RemoveFromCacheAsync(repositoryId);
            }
        }

        // Create new repository and cache it
        await _cacheLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_repositoryCache.TryGetValue(repositoryId, out cached) && IsValidCache(cached))
            {
                cached.LastAccessed = DateTime.UtcNow;
                return cached.Repository;
            }

            _logger?.LogInfo("GitManager", $"Creating and caching new repository instance: {repositoryId}");

            var repository = new GitRepository(config, _logger);
            var cachedRepo = new CachedRepository
            {
                Repository = repository,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow
            };

            _repositoryCache[repositoryId] = cachedRepo;

            return repository;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Gets repository configuration by ID
    /// </summary>
    /// <param name="repositoryId">Repository configuration ID</param>
    /// <returns>Repository configuration or null if not found</returns>
    public RepositoryConfiguration? GetConfiguration(string repositoryId = "Default")
    {
        return _configuration.GetRepository(repositoryId);
    }

    /// <summary>
    /// Gets all repository configuration IDs
    /// </summary>
    /// <returns>List of repository IDs</returns>
    public List<string> GetRepositoryIds()
    {
        return _configuration.Repositories.Select(r => r.Id).ToList();
    }

    #endregion

    #region Configuration Management

    /// <summary>
    /// Registers a new repository configuration
    /// </summary>
    /// <param name="configuration">Repository configuration to register</param>
    /// <exception cref="ArgumentException">If configuration with same ID already exists</exception>
    public void RegisterRepository(RepositoryConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (!configuration.IsValid())
        {
            throw new ArgumentException("Invalid repository configuration", nameof(configuration));
        }

        // Check if already exists
        if (_configuration.GetRepository(configuration.Id) != null)
        {
            throw new ArgumentException($"Repository configuration already exists: {configuration.Id}", nameof(configuration));
        }

        _configuration.Repositories.Add(configuration);

        _logger?.LogInfo("GitManager", $"Registered new repository configuration: {configuration.Id}");
    }

    /// <summary>
    /// Unregisters a repository configuration and removes it from cache
    /// </summary>
    /// <param name="repositoryId">Repository ID to unregister</param>
    /// <returns>True if unregistered successfully</returns>
    public async Task<bool> UnregisterRepositoryAsync(string repositoryId)
    {
        // Remove from cache first
        await RemoveFromCacheAsync(repositoryId);

        // Remove from configuration
        var config = _configuration.GetRepository(repositoryId);
        if (config != null)
        {
            _configuration.Repositories.Remove(config);
            _logger?.LogInfo("GitManager", $"Unregistered repository configuration: {repositoryId}");
            return true;
        }

        return false;
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Removes a repository from cache
    /// </summary>
    /// <param name="repositoryId">Repository ID to remove</param>
    public async Task RemoveFromCacheAsync(string repositoryId)
    {
        await _cacheLock.WaitAsync();
        try
        {
            if (_repositoryCache.TryRemove(repositoryId, out var cached))
            {
                cached.Repository?.Dispose();
                _logger?.LogDebug("GitManager", $"Removed repository from cache: {repositoryId}");
            }
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Clears all cached repositories
    /// </summary>
    public async Task ClearCacheAsync()
    {
        await _cacheLock.WaitAsync();
        try
        {
            foreach (var cached in _repositoryCache.Values)
            {
                cached.Repository?.Dispose();
            }

            _repositoryCache.Clear();

            _logger?.LogInfo("GitManager", "Cleared all cached repositories");
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    /// <returns>Cache statistics</returns>
    public CacheStatistics GetCacheStatistics()
    {
        var stats = new CacheStatistics
        {
            TotalCachedRepositories = _repositoryCache.Count,
            CacheEnabled = _configuration.EnableRepositoryCaching,
            CacheTimeoutMinutes = _configuration.CacheTimeoutMinutes
        };

        foreach (var cached in _repositoryCache.Values)
        {
            stats.Repositories.Add(new CachedRepositoryInfo
            {
                Age = DateTime.UtcNow - cached.CreatedAt,
                TimeSinceLastAccess = DateTime.UtcNow - cached.LastAccessed,
                IsExpired = !IsValidCache(cached)
            });
        }

        return stats;
    }

    /// <summary>
    /// Checks if a cached repository is still valid
    /// </summary>
    private bool IsValidCache(CachedRepository cached)
    {
        if (_configuration.CacheTimeoutMinutes <= 0)
        {
            return true; // No timeout
        }

        var age = DateTime.UtcNow - cached.CreatedAt;
        return age.TotalMinutes < _configuration.CacheTimeoutMinutes;
    }

    /// <summary>
    /// Timer callback to cleanup expired caches
    /// </summary>
    private void CleanupExpiredCaches(object? state)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            var expiredIds = new List<string>();

            // Find expired repositories
            foreach (var kvp in _repositoryCache)
            {
                if (!IsValidCache(kvp.Value))
                {
                    expiredIds.Add(kvp.Key);
                }
            }

            // Remove expired repositories
            if (expiredIds.Count > 0)
            {
                _logger?.LogDebug("GitManager", $"Cleaning up {expiredIds.Count} expired cache(s)");

                foreach (var id in expiredIds)
                {
                    if (_repositoryCache.TryRemove(id, out var cached))
                    {
                        cached.Repository?.Dispose();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("GitManager", $"Error during cache cleanup: {ex.Message}", ex);
        }
    }

    #endregion

    #region Batch Operations

    /// <summary>
    /// Performs an operation on all configured repositories
    /// </summary>
    /// <param name="operation">Operation to perform on each repository</param>
    /// <param name="maxConcurrency">Maximum concurrent operations (0 = use configuration default)</param>
    /// <returns>Dictionary of results keyed by repository ID</returns>
    public async Task<Dictionary<string, OperationResult>> ExecuteOnAllAsync(
        Func<GitRepository, string, Task<OperationResult>> operation,
        int maxConcurrency = 0)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        var concurrency = maxConcurrency > 0 ? maxConcurrency : _configuration.MaxConcurrentOperations;
        var results = new ConcurrentDictionary<string, OperationResult>();
        var semaphore = new SemaphoreSlim(concurrency, concurrency);

        _logger?.LogInfo("GitManager",
            $"Executing batch operation on {_configuration.Repositories.Count} repository(ies) (max concurrency: {concurrency})");

        var tasks = _configuration.Repositories.Select(async config =>
        {
            await semaphore.WaitAsync();
            try
            {
                var repository = await GetRepositoryAsync(config.Id);
                var result = await operation(repository, config.Id);
                results[config.Id] = result;
            }
            catch (Exception ex)
            {
                _logger?.LogError("GitManager",
                    $"Error executing operation on repository '{config.Id}': {ex.Message}", ex);

                results[config.Id] = new OperationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        var successCount = results.Values.Count(r => r.Success);
        _logger?.LogInfo("GitManager",
            $"Batch operation completed: {successCount}/{results.Count} succeeded");

        return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Fetches all configured repositories
    /// </summary>
    /// <param name="fetchOptions">Fetch options (optional)</param>
    /// <param name="maxConcurrency">Maximum concurrent operations</param>
    /// <returns>Dictionary of results keyed by repository ID</returns>
    public async Task<Dictionary<string, OperationResult>> FetchAllAsync(
        FetchOptions? fetchOptions = null,
        int maxConcurrency = 0)
    {
        return await ExecuteOnAllAsync(async (repo, id) =>
        {
            var result = await repo.FetchAsync(fetchOptions);
            return new OperationResult
            {
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                Exception = result.Exception,
                Diagnostics = result.Diagnostics
            };
        }, maxConcurrency);
    }

    /// <summary>
    /// Gets status for all configured repositories
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations</param>
    /// <returns>Dictionary of repository statuses keyed by repository ID</returns>
    public async Task<Dictionary<string, GitOperationResult<RepositoryStatus>>> GetAllStatusAsync(
        int maxConcurrency = 0)
    {
        var concurrency = maxConcurrency > 0 ? maxConcurrency : _configuration.MaxConcurrentOperations;
        var results = new ConcurrentDictionary<string, GitOperationResult<RepositoryStatus>>();
        var semaphore = new SemaphoreSlim(concurrency, concurrency);

        var tasks = _configuration.Repositories.Select(async config =>
        {
            await semaphore.WaitAsync();
            try
            {
                var repository = await GetRepositoryAsync(config.Id);
                var status = await repository.GetStatusAsync();
                results[config.Id] = status;
            }
            catch (Exception ex)
            {
                results[config.Id] = GitOperationResult<RepositoryStatus>.Fail(ex.Message, ex);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    #endregion

    #region Health Checks

    /// <summary>
    /// Performs health check on all repositories
    /// </summary>
    /// <returns>Health check results for all repositories</returns>
    public async Task<Dictionary<string, bool>> HealthCheckAsync()
    {
        var results = new Dictionary<string, bool>();

        _logger?.LogInfo("GitManager", $"Performing health check on {_configuration.Repositories.Count} repository(ies)");

        foreach (var config in _configuration.Repositories)
        {
            try
            {
                var repository = await GetRepositoryAsync(config.Id);
                var info = await repository.GetRepositoryInfoAsync();
                results[config.Id] = info.Success;

                if (info.Success)
                {
                    _logger?.LogSuccess("GitManager", $"✓ Repository '{config.Id}' is healthy");
                }
                else
                {
                    _logger?.LogError("GitManager", $"✗ Repository '{config.Id}' health check failed: {info.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("GitManager", $"✗ Repository '{config.Id}' health check error: {ex.Message}", ex);
                results[config.Id] = false;
            }
        }

        var healthyCount = results.Values.Count(h => h);
        _logger?.LogInfo("GitManager", $"Health check completed: {healthyCount}/{results.Count} healthy");

        return results;
    }

    #endregion

    #region Dispose

    /// <summary>
    /// Disposes the manager and all cached repositories
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger?.LogInfo("GitManager", "Disposing GitManager and clearing caches");

        // Stop cleanup timer
        _cleanupTimer?.Dispose();

        // Dispose all cached repositories
        foreach (var cached in _repositoryCache.Values)
        {
            try
            {
                cached.Repository?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError("GitManager", $"Error disposing repository: {ex.Message}", ex);
            }
        }

        _repositoryCache.Clear();
        _cacheLock?.Dispose();

        _disposed = true;

        _logger?.LogSuccess("GitManager", "GitManager disposed successfully");
    }

    #endregion
}

/// <summary>
/// Represents a cached repository instance
/// </summary>
internal class CachedRepository
{
    /// <summary>
    /// The repository instance
    /// </summary>
    public required GitRepository Repository { get; init; }

    /// <summary>
    /// When this cache entry was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When this cache entry was last accessed
    /// </summary>
    public DateTime LastAccessed { get; set; }
}

/// <summary>
/// Result of a batch operation
/// </summary>
public class OperationResult
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Exception details (if available)
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Operation diagnostics
    /// </summary>
    public OperationDiagnostics? Diagnostics { get; set; }
}

/// <summary>
/// Cache statistics
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Total number of cached repositories
    /// </summary>
    public int TotalCachedRepositories { get; set; }

    /// <summary>
    /// Is caching enabled
    /// </summary>
    public bool CacheEnabled { get; set; }

    /// <summary>
    /// Cache timeout in minutes
    /// </summary>
    public int CacheTimeoutMinutes { get; set; }

    /// <summary>
    /// Information about each cached repository
    /// </summary>
    public List<CachedRepositoryInfo> Repositories { get; set; } = new();
}

/// <summary>
/// Information about a cached repository
/// </summary>
public class CachedRepositoryInfo
{
    /// <summary>
    /// Age of the cache entry
    /// </summary>
    public TimeSpan Age { get; set; }

    /// <summary>
    /// Time since last access
    /// </summary>
    public TimeSpan TimeSinceLastAccess { get; set; }

    /// <summary>
    /// Is this cache entry expired
    /// </summary>
    public bool IsExpired { get; set; }
}
