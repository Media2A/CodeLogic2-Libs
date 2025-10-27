using Amazon.S3;
using Amazon.S3.Model;
using CodeLogic.Abstractions;
using CL.StorageS3.Models;

namespace CL.StorageS3.Services;

/// <summary>
/// Manages S3 client connections with caching and lifecycle management
/// </summary>
public class S3ConnectionManager : IDisposable
{
    private readonly ILogger? _logger;
    private readonly Dictionary<string, S3Configuration> _configurations;
    private readonly Dictionary<string, AmazonS3Client> _clientCache;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of S3ConnectionManager
    /// </summary>
    /// <param name="logger">Optional logger instance</param>
    public S3ConnectionManager(ILogger? logger = null)
    {
        _logger = logger;
        _configurations = new Dictionary<string, S3Configuration>(StringComparer.OrdinalIgnoreCase);
        _clientCache = new Dictionary<string, AmazonS3Client>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Registers a new S3 configuration
    /// </summary>
    /// <param name="configuration">S3 configuration to register</param>
    public void RegisterConfiguration(S3Configuration configuration)
    {
        if (configuration == null)
        {
            _logger?.LogError("S3ConnectionManager", "Cannot register null configuration");
            throw new ArgumentNullException(nameof(configuration));
        }

        if (string.IsNullOrWhiteSpace(configuration.ConnectionId))
        {
            _logger?.LogError("S3ConnectionManager", "Configuration must have a ConnectionId");
            throw new ArgumentException("Configuration must have a ConnectionId", nameof(configuration));
        }

        if (!configuration.IsValid())
        {
            _logger?.LogError("S3ConnectionManager", $"Invalid configuration for '{configuration.ConnectionId}'");
            throw new ArgumentException($"Configuration '{configuration.ConnectionId}' is not valid", nameof(configuration));
        }

        lock (_lock)
        {
            _configurations[configuration.ConnectionId] = configuration;
            _logger?.LogInfo("S3ConnectionManager", $"Registered S3 configuration: {configuration.ConnectionId}");
        }
    }

    /// <summary>
    /// Gets or creates an S3 client for the specified connection
    /// </summary>
    /// <param name="connectionId">Connection identifier</param>
    /// <returns>AmazonS3Client instance</returns>
    public AmazonS3Client GetClient(string connectionId = "Default")
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(S3ConnectionManager));
        }

        lock (_lock)
        {
            // Return cached client if available
            if (_clientCache.TryGetValue(connectionId, out var cachedClient))
            {
                return cachedClient;
            }

            // Get configuration
            if (!_configurations.TryGetValue(connectionId, out var config))
            {
                _logger?.LogError("S3ConnectionManager", $"No configuration found for connection: {connectionId}");
                throw new InvalidOperationException($"No S3 configuration registered for '{connectionId}'");
            }

            // Build new client
            _logger?.LogInfo("S3ConnectionManager", $"Creating new S3 client for: {connectionId}");
            var client = config.BuildClient();

            // Cache the client
            _clientCache[connectionId] = client;

            return client;
        }
    }

    /// <summary>
    /// Tests connection to S3 service
    /// </summary>
    /// <param name="connectionId">Connection identifier</param>
    /// <returns>True if connection is successful</returns>
    public async Task<bool> TestConnectionAsync(string connectionId = "Default")
    {
        try
        {
            _logger?.LogInfo("S3ConnectionManager", $"Testing S3 connection: {connectionId}");

            var client = GetClient(connectionId);

            // Try to list buckets as a connection test
            var response = await client.ListBucketsAsync();

            _logger?.LogSuccess("S3ConnectionManager",
                $"S3 connection '{connectionId}' successful - Found {response.Buckets.Count} buckets");

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3ConnectionManager",
                $"S3 connection '{connectionId}' failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tests bucket accessibility
    /// </summary>
    /// <param name="bucketName">Bucket name to test</param>
    /// <param name="connectionId">Connection identifier</param>
    /// <returns>True if bucket is accessible</returns>
    public async Task<bool> TestBucketAccessAsync(string bucketName, string connectionId = "Default")
    {
        try
        {
            _logger?.LogInfo("S3ConnectionManager", $"Testing bucket access: {bucketName} on {connectionId}");

            var client = GetClient(connectionId);

            // Check if bucket exists and is accessible
            var request = new GetBucketLocationRequest
            {
                BucketName = bucketName
            };

            var response = await client.GetBucketLocationAsync(request);

            _logger?.LogSuccess("S3ConnectionManager",
                $"Bucket '{bucketName}' is accessible - Region: {response.Location}");

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError("S3ConnectionManager",
                $"Bucket '{bucketName}' access failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets configuration for a connection
    /// </summary>
    /// <param name="connectionId">Connection identifier</param>
    /// <returns>S3Configuration or null</returns>
    public S3Configuration? GetConfiguration(string connectionId = "Default")
    {
        lock (_lock)
        {
            return _configurations.TryGetValue(connectionId, out var config) ? config : null;
        }
    }

    /// <summary>
    /// Gets all registered connection IDs
    /// </summary>
    /// <returns>List of connection IDs</returns>
    public List<string> GetConnectionIds()
    {
        lock (_lock)
        {
            return _configurations.Keys.ToList();
        }
    }

    /// <summary>
    /// Closes and removes a specific client from cache
    /// </summary>
    /// <param name="connectionId">Connection identifier</param>
    public void CloseConnection(string connectionId)
    {
        lock (_lock)
        {
            if (_clientCache.TryGetValue(connectionId, out var client))
            {
                client.Dispose();
                _clientCache.Remove(connectionId);
                _logger?.LogInfo("S3ConnectionManager", $"Closed S3 connection: {connectionId}");
            }
        }
    }

    /// <summary>
    /// Disposes all S3 clients and clears caches
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            _logger?.LogInfo("S3ConnectionManager", "Disposing S3ConnectionManager");

            foreach (var kvp in _clientCache)
            {
                try
                {
                    kvp.Value.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogError("S3ConnectionManager",
                        $"Error disposing client '{kvp.Key}': {ex.Message}");
                }
            }

            _clientCache.Clear();
            _configurations.Clear();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
