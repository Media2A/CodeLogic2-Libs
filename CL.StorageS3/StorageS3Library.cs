using CodeLogic.Abstractions;
using CL.Core.Models;
using CL.StorageS3.Models;
using CL.StorageS3.Services;
using CodeLogic.Configuration;
using System.Text.Json;

namespace CL.StorageS3;

/// <summary>
/// S3 Storage Library for CodeLogic Framework
/// Provides Amazon S3 and S3-compatible storage operations
/// </summary>
public class StorageS3Library : ILibrary
{
    private ILogger? _logger;
    private LibraryContext? _context;
    private S3ConnectionManager? _connectionManager;
    private bool _initialized;

    /// <summary>
    /// Library manifest information
    /// </summary>
    public ILibraryManifest Manifest => new StorageS3Manifest();

    #region Lifecycle Methods

    /// <summary>
    /// Called when the library is loaded into the framework
    /// </summary>
    public async Task OnLoadAsync(LibraryContext context)
    {
        _context = context;
        _logger = (ILogger)context.Logger;

        _logger?.LogInfo("StorageS3Library", "Loading CL.StorageS3 library");

        await LoadS3ConfigurationsAsync();

        _logger?.LogSuccess("StorageS3Library", "CL.StorageS3 library loaded successfully");
    }

    /// <summary>
    /// Called when the library should initialize and prepare for use
    /// </summary>
    public async Task OnInitializeAsync()
    {
        if (_initialized)
        {
            _logger?.LogWarning("StorageS3Library", "Library already initialized");
            return;
        }

        _logger?.LogInfo("StorageS3Library", "Initializing CL.StorageS3 library");

        // Test S3 connections
        await TestS3ConnectionsAsync();

        _initialized = true;

        _logger?.LogSuccess("StorageS3Library", "CL.StorageS3 library initialized successfully");
    }

    /// <summary>
    /// Called when the library is being unloaded from the framework
    /// </summary>
    public async Task OnUnloadAsync()
    {
        _logger?.LogInfo("StorageS3Library", "Unloading CL.StorageS3 library");

        // Dispose connection manager
        _connectionManager?.Dispose();
        _connectionManager = null;

        _initialized = false;

        _logger?.LogSuccess("StorageS3Library", "CL.StorageS3 library unloaded successfully");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Performs health check on all registered S3 connections
    /// </summary>
    public async Task<HealthCheckResult> HealthCheckAsync()
    {
        if (_connectionManager == null)
        {
            return new HealthCheckResult
            {
                IsHealthy = false,
                Message = "Connection manager not initialized"
            };
        }

        try
        {
            var connectionIds = _connectionManager.GetConnectionIds();

            if (connectionIds.Count == 0)
            {
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Message = "No S3 connections configured"
                };
            }

            var results = new List<string>();
            var allHealthy = true;

            foreach (var connectionId in connectionIds)
            {
                var isHealthy = await _connectionManager.TestConnectionAsync(connectionId);

                if (!isHealthy)
                {
                    allHealthy = false;
                    results.Add($"{connectionId}: Failed");
                }
                else
                {
                    results.Add($"{connectionId}: OK");
                }
            }

            var healthyCount = results.Count(r => r.EndsWith("OK"));
            var detailMessage = $"{string.Join(", ", results)} (Total: {connectionIds.Count}, Healthy: {healthyCount})";

            return new HealthCheckResult
            {
                IsHealthy = allHealthy,
                Message = detailMessage
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError("StorageS3Library", $"Health check failed: {ex.Message}");

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
    /// Loads S3 configurations from the CodeLogic configuration system
    /// </summary>
    private async Task LoadS3ConfigurationsAsync()
    {
        try
        {
            if (_context == null)
            {
                _logger?.LogError("StorageS3Library", "Library context is null");
                return;
            }

            var configManager = _context.Services.GetService(typeof(ConfigurationManager)) as ConfigurationManager;

            if (configManager == null)
            {
                _logger?.LogError("StorageS3Library", "ConfigurationManager service not available");
                return;
            }

            // Initialize connection manager
            _connectionManager = new S3ConnectionManager(_logger);

            // Try to load configuration
            var configPath = Path.Combine("config", "s3.json");

            StorageS3Configuration? config = null;

            if (File.Exists(configPath))
            {
                _logger?.LogInfo("StorageS3Library", $"Loading S3 configuration from: {configPath}");

                var json = await File.ReadAllTextAsync(configPath);
                config = JsonSerializer.Deserialize<StorageS3Configuration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });
            }

            if (config == null || config.Connections.Count == 0)
            {
                _logger?.LogWarning("StorageS3Library", "No S3 configuration found, creating default template");

                // Create default configuration template
                config = StorageS3Configuration.GetDefaultTemplate();

                // Save template
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                await File.WriteAllTextAsync(configPath, json);

                _logger?.LogInfo("StorageS3Library", $"Created default S3 configuration at: {configPath}");
            }

            // Register all configurations
            foreach (var s3Config in config.Connections)
            {
                try
                {
                    _connectionManager.RegisterConfiguration(s3Config);
                    _logger?.LogInfo("StorageS3Library",
                        $"Registered S3 configuration: {s3Config.ConnectionId}");
                }
                catch (Exception ex)
                {
                    _logger?.LogError("StorageS3Library",
                        $"Failed to register S3 configuration '{s3Config.ConnectionId}': {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("StorageS3Library", $"Failed to load S3 configurations: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests all registered S3 connections
    /// </summary>
    private async Task TestS3ConnectionsAsync()
    {
        if (_connectionManager == null)
        {
            _logger?.LogWarning("StorageS3Library", "Connection manager not initialized");
            return;
        }

        var connectionIds = _connectionManager.GetConnectionIds();

        if (connectionIds.Count == 0)
        {
            _logger?.LogWarning("StorageS3Library", "No S3 connections configured");
            return;
        }

        _logger?.LogInfo("StorageS3Library", $"Testing {connectionIds.Count} S3 connection(s)...");

        foreach (var connectionId in connectionIds)
        {
            var success = await _connectionManager.TestConnectionAsync(connectionId);

            if (success)
            {
                var config = _connectionManager.GetConfiguration(connectionId);

                _logger?.LogSuccess("StorageS3Library",
                    $"✓ Connection '{connectionId}' successful - Service: {config?.ServiceUrl}");

                // Test default bucket access if configured
                if (!string.IsNullOrWhiteSpace(config?.DefaultBucket))
                {
                    var bucketAccessible = await _connectionManager.TestBucketAccessAsync(
                        config.DefaultBucket, connectionId);

                    if (bucketAccessible)
                    {
                        _logger?.LogSuccess("StorageS3Library",
                            $"  ✓ Default bucket '{config.DefaultBucket}' is accessible");
                    }
                    else
                    {
                        _logger?.LogWarning("StorageS3Library",
                            $"  ⚠ Default bucket '{config.DefaultBucket}' is not accessible");
                    }
                }
            }
            else
            {
                _logger?.LogError("StorageS3Library", $"✗ Connection '{connectionId}' failed");
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gets a storage service for S3 operations
    /// </summary>
    /// <param name="connectionId">Connection identifier (default: "Default")</param>
    /// <returns>S3StorageService instance</returns>
    /// <exception cref="InvalidOperationException">If connection manager is not initialized</exception>
    public S3StorageService? GetStorageService(string connectionId = "Default")
    {
        if (_connectionManager == null)
        {
            _logger?.LogError("StorageS3Library", "Connection manager not initialized");
            return null;
        }

        try
        {
            return new S3StorageService(_connectionManager, connectionId, _logger);
        }
        catch (Exception ex)
        {
            _logger?.LogError("StorageS3Library",
                $"Failed to create storage service for '{connectionId}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Registers a new S3 bucket configuration
    /// </summary>
    /// <param name="configuration">S3 configuration to register</param>
    public void RegisterBucket(S3Configuration configuration)
    {
        if (_connectionManager == null)
        {
            _logger?.LogError("StorageS3Library", "Connection manager not initialized");
            throw new InvalidOperationException("Connection manager not initialized");
        }

        _connectionManager.RegisterConfiguration(configuration);
    }

    /// <summary>
    /// Gets the connection manager instance
    /// </summary>
    /// <returns>S3ConnectionManager instance</returns>
    public S3ConnectionManager? GetConnectionManager()
    {
        return _connectionManager;
    }

    /// <summary>
    /// Gets all registered connection IDs
    /// </summary>
    /// <returns>List of connection IDs</returns>
    public List<string> GetConnectionIds()
    {
        return _connectionManager?.GetConnectionIds() ?? new List<string>();
    }

    #endregion
}

/// <summary>
/// Library manifest for StorageS3Library
/// </summary>
public class StorageS3Manifest : ILibraryManifest
{
    public string Id => "cl.storages3";
    public string Name => "CL.StorageS3";
    public string Version => "2.0.0";
    public string Description => "Amazon S3 and S3-compatible storage library for CodeLogic Framework";
    public string Author => "Media2A";

    public IReadOnlyList<LibraryDependency> Dependencies => new List<LibraryDependency>();

    public IReadOnlyList<string> Tags => new List<string>
    {
        "Storage", "S3", "Cloud", "AWS"
    };
}
