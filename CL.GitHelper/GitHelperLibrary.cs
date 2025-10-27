using CodeLogic.Abstractions;
using CL.Core.Models;
using CL.GitHelper.Models;
using CL.GitHelper.Services;
using CodeLogic.Configuration;
using System.Text.Json;

namespace CL.GitHelper;

/// <summary>
/// Git Helper Library for CodeLogic Framework
/// Provides Git repository operations with LibGit2Sharp integration
/// </summary>
public class GitHelperLibrary : ILibrary
{
    private ILogger? _logger;
    private LibraryContext? _context;
    private GitManager? _gitManager;
    private bool _initialized;

    /// <summary>
    /// Library manifest information
    /// </summary>
    public ILibraryManifest Manifest => new GitHelperManifest();

    #region Lifecycle Methods

    /// <summary>
    /// Called when the library is loaded into the framework
    /// </summary>
    public async Task OnLoadAsync(LibraryContext context)
    {
        _context = context;
        _logger = (ILogger)context.Logger;

        _logger?.LogInfo("GitHelperLibrary", "Loading CL.GitHelper library");

        await LoadGitConfigurationsAsync();

        _logger?.LogSuccess("GitHelperLibrary", "CL.GitHelper library loaded successfully");
    }

    /// <summary>
    /// Called when the library should initialize and prepare for use
    /// </summary>
    public async Task OnInitializeAsync()
    {
        if (_initialized)
        {
            _logger?.LogWarning("GitHelperLibrary", "Library already initialized");
            return;
        }

        _logger?.LogInfo("GitHelperLibrary", "Initializing CL.GitHelper library");

        // Test Git repository connections
        await TestRepositoryConnectionsAsync();

        _initialized = true;

        _logger?.LogSuccess("GitHelperLibrary", "CL.GitHelper library initialized successfully");
    }

    /// <summary>
    /// Called when the library is being unloaded from the framework
    /// </summary>
    public async Task OnUnloadAsync()
    {
        _logger?.LogInfo("GitHelperLibrary", "Unloading CL.GitHelper library");

        // Dispose Git manager
        _gitManager?.Dispose();
        _gitManager = null;

        _initialized = false;

        _logger?.LogSuccess("GitHelperLibrary", "CL.GitHelper library unloaded successfully");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Performs health check on all registered Git repositories
    /// </summary>
    public async Task<HealthCheckResult> HealthCheckAsync()
    {
        if (_gitManager == null)
        {
            return new HealthCheckResult
            {
                IsHealthy = false,
                Message = "Git manager not initialized"
            };
        }

        try
        {
            var results = await _gitManager.HealthCheckAsync();

            if (results.Count == 0)
            {
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Message = "No Git repositories configured"
                };
            }

            var healthyCount = results.Values.Count(h => h);
            var allHealthy = healthyCount == results.Count;

            var statusMessages = results.Select(r => $"{r.Key}: {(r.Value ? "OK" : "Failed")}");
            var detailMessage = $"{string.Join(", ", statusMessages)} (Total: {results.Count}, Healthy: {healthyCount})";

            return new HealthCheckResult
            {
                IsHealthy = allHealthy,
                Message = detailMessage
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError("GitHelperLibrary", $"Health check failed: {ex.Message}", ex);

            return new HealthCheckResult
            {
                IsHealthy = false,
                Message = $"Health check error: {ex.Message}"
            };
        }
    }

    #endregion

    #region Configuration Management

    /// <summary>
    /// Loads Git configurations from the CodeLogic configuration system
    /// </summary>
    private async Task LoadGitConfigurationsAsync()
    {
        try
        {
            if (_context == null)
            {
                _logger?.LogError("GitHelperLibrary", "Library context is null");
                return;
            }

            var configManager = _context.Services.GetService(typeof(ConfigurationManager)) as ConfigurationManager;

            if (configManager == null)
            {
                _logger?.LogError("GitHelperLibrary", "ConfigurationManager service not available");
                return;
            }

            // Try to load configuration
            var configPath = Path.Combine("config", "git.json");

            GitHelperConfiguration? config = null;

            if (File.Exists(configPath))
            {
                _logger?.LogInfo("GitHelperLibrary", $"Loading Git configuration from: {configPath}");

                var json = await File.ReadAllTextAsync(configPath);
                config = JsonSerializer.Deserialize<GitHelperConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });
            }

            if (config == null || config.Repositories.Count == 0)
            {
                _logger?.LogWarning("GitHelperLibrary", "No Git configuration found, creating default template");

                // Create default configuration template
                config = GitHelperConfiguration.GetDefaultTemplate();

                // Save template
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                await File.WriteAllTextAsync(configPath, json);

                _logger?.LogInfo("GitHelperLibrary", $"Created default Git configuration at: {configPath}");
            }

            // Initialize Git manager
            _gitManager = new GitManager(config, _logger);

            _logger?.LogInfo("GitHelperLibrary",
                $"Initialized GitManager with {config.Repositories.Count} repository configuration(s)");
        }
        catch (Exception ex)
        {
            _logger?.LogError("GitHelperLibrary", $"Failed to load Git configurations: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tests all registered Git repository connections
    /// </summary>
    private async Task TestRepositoryConnectionsAsync()
    {
        if (_gitManager == null)
        {
            _logger?.LogWarning("GitHelperLibrary", "Git manager not initialized");
            return;
        }

        var repositoryIds = _gitManager.GetRepositoryIds();

        if (repositoryIds.Count == 0)
        {
            _logger?.LogWarning("GitHelperLibrary", "No Git repositories configured");
            return;
        }

        _logger?.LogInfo("GitHelperLibrary", $"Testing {repositoryIds.Count} Git repository(ies)...");

        foreach (var repositoryId in repositoryIds)
        {
            try
            {
                var config = _gitManager.GetConfiguration(repositoryId);
                if (config == null)
                {
                    _logger?.LogError("GitHelperLibrary", $"✗ Repository configuration not found: {repositoryId}");
                    continue;
                }

                // Get or create repository instance
                var repository = await _gitManager.GetRepositoryAsync(repositoryId);

                // Check if repository exists locally
                var localPath = config.UseAppDataDir
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "CodeLogic", config.LocalPath)
                    : config.LocalPath;

                if (!Directory.Exists(localPath) || !Directory.Exists(Path.Combine(localPath, ".git")))
                {
                    _logger?.LogWarning("GitHelperLibrary",
                        $"⚠ Repository '{repositoryId}' not cloned yet - Local path: {localPath}");
                    _logger?.LogInfo("GitHelperLibrary",
                        $"  To clone, call CloneAsync() on the repository instance");
                }
                else
                {
                    // Try to get repository info
                    var info = await repository.GetRepositoryInfoAsync();

                    if (info.Success)
                    {
                        _logger?.LogSuccess("GitHelperLibrary",
                            $"✓ Repository '{repositoryId}' accessible - Branch: {info.Data?.CurrentBranch}, Path: {localPath}");

                        if (info.Data?.IsDirty == true)
                        {
                            _logger?.LogWarning("GitHelperLibrary",
                                $"  ⚠ Repository has uncommitted changes ({info.Data.ModifiedFiles} modified, {info.Data.UntrackedFiles} untracked)");
                        }
                    }
                    else
                    {
                        _logger?.LogError("GitHelperLibrary",
                            $"✗ Failed to access repository '{repositoryId}': {info.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("GitHelperLibrary",
                    $"✗ Error testing repository '{repositoryId}': {ex.Message}", ex);
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gets a Git repository instance
    /// </summary>
    /// <param name="repositoryId">Repository identifier (default: "Default")</param>
    /// <returns>GitRepository instance</returns>
    /// <exception cref="InvalidOperationException">If Git manager is not initialized</exception>
    public async Task<GitRepository?> GetRepositoryAsync(string repositoryId = "Default")
    {
        if (_gitManager == null)
        {
            _logger?.LogError("GitHelperLibrary", "Git manager not initialized");
            return null;
        }

        try
        {
            return await _gitManager.GetRepositoryAsync(repositoryId);
        }
        catch (Exception ex)
        {
            _logger?.LogError("GitHelperLibrary",
                $"Failed to get repository '{repositoryId}': {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets the Git manager instance
    /// </summary>
    /// <returns>GitManager instance</returns>
    public GitManager? GetGitManager()
    {
        return _gitManager;
    }

    /// <summary>
    /// Registers a new Git repository configuration
    /// </summary>
    /// <param name="configuration">Repository configuration to register</param>
    public void RegisterRepository(RepositoryConfiguration configuration)
    {
        if (_gitManager == null)
        {
            _logger?.LogError("GitHelperLibrary", "Git manager not initialized");
            throw new InvalidOperationException("Git manager not initialized");
        }

        _gitManager.RegisterRepository(configuration);
    }

    /// <summary>
    /// Gets all registered repository IDs
    /// </summary>
    /// <returns>List of repository IDs</returns>
    public List<string> GetRepositoryIds()
    {
        return _gitManager?.GetRepositoryIds() ?? new List<string>();
    }

    /// <summary>
    /// Gets cache statistics from the Git manager
    /// </summary>
    /// <returns>Cache statistics</returns>
    public CacheStatistics? GetCacheStatistics()
    {
        return _gitManager?.GetCacheStatistics();
    }

    /// <summary>
    /// Clears the repository cache
    /// </summary>
    public async Task ClearCacheAsync()
    {
        if (_gitManager != null)
        {
            await _gitManager.ClearCacheAsync();
            _logger?.LogInfo("GitHelperLibrary", "Repository cache cleared");
        }
    }

    /// <summary>
    /// Fetches updates for all configured repositories
    /// </summary>
    /// <param name="fetchOptions">Optional fetch options</param>
    /// <param name="maxConcurrency">Maximum concurrent operations</param>
    /// <returns>Dictionary of operation results keyed by repository ID</returns>
    public async Task<Dictionary<string, OperationResult>?> FetchAllAsync(
        FetchOptions? fetchOptions = null,
        int maxConcurrency = 0)
    {
        if (_gitManager == null)
        {
            _logger?.LogError("GitHelperLibrary", "Git manager not initialized");
            return null;
        }

        return await _gitManager.FetchAllAsync(fetchOptions, maxConcurrency);
    }

    /// <summary>
    /// Gets status for all configured repositories
    /// </summary>
    /// <param name="maxConcurrency">Maximum concurrent operations</param>
    /// <returns>Dictionary of repository statuses keyed by repository ID</returns>
    public async Task<Dictionary<string, GitOperationResult<RepositoryStatus>>?> GetAllStatusAsync(
        int maxConcurrency = 0)
    {
        if (_gitManager == null)
        {
            _logger?.LogError("GitHelperLibrary", "Git manager not initialized");
            return null;
        }

        return await _gitManager.GetAllStatusAsync(maxConcurrency);
    }

    #endregion
}

/// <summary>
/// Library manifest for GitHelperLibrary
/// </summary>
public class GitHelperManifest : ILibraryManifest
{
    public string Id => "cl.githelper";
    public string Name => "CL.GitHelper";
    public string Version => "2.0.0";
    public string Description => "Git repository management library for CodeLogic Framework with LibGit2Sharp integration";
    public string Author => "Media2A";

    public IReadOnlyList<LibraryDependency> Dependencies => new List<LibraryDependency>();

    public IReadOnlyList<string> Tags => new List<string>
    {
        "Git", "VCS", "Repository", "Source Control"
    };
}
