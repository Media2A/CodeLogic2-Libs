using CodeLogic.Abstractions;
using CodeLogic.Models;
using CL.SQLite.Models;
using CL.SQLite.Services;
using Newtonsoft.Json.Linq;

namespace CL.SQLite;

/// <summary>
/// SQLite library implementation for CodeLogic framework
/// </summary>
public class SQLiteLibrary : ILibrary
{
    private ConnectionManager? _connectionManager;
    private TableSyncService? _tableSyncService;
    private ILogger? _logger;
    private SQLiteConfiguration? _config;
    private string? _dataDirectory;
    private bool _initialized;

    public ILibraryManifest Manifest { get; } = new SQLiteManifest();

    /// <summary>
    /// Gets the TableSyncService for schema synchronization.
    /// </summary>
    public TableSyncService? TableSyncService => _tableSyncService;

    public Task OnLoadAsync(LibraryContext context)
    {
        _logger = context.Logger;
        _dataDirectory = context.DataDirectory;
        _logger.Info($"Loading {Manifest.Name} v{Manifest.Version}");

        // Try to load configuration from sqlite.json file
        _config = LoadConfigurationFromFile(context);

        // If no file config, try from CodeLogic configuration
        if (_config == null)
        {
            _config = context.Configuration.TryGetValue("SQLite", out var configObj) && configObj is SQLiteConfiguration sqliteConfig
                ? sqliteConfig
                : new SQLiteConfiguration();
        }

        _logger.Info($"SQLite configuration loaded: DatabasePath={_config.DatabasePath}, MaxPoolSize={_config.MaxPoolSize}");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads SQLite configuration from the sqlite.json config file
    /// </summary>
    private SQLiteConfiguration? LoadConfigurationFromFile(LibraryContext context)
    {
        try
        {
            string sqliteConfigPath = "";

            // Construct path from DataDirectory (which is like bin/Debug/net10.0/data/cl.sqlite)
            // and go up to find the global config directory
            if (!string.IsNullOrEmpty(context.DataDirectory))
            {
                var dataDir = context.DataDirectory;
                // Remove the 'data/cl.sqlite' part to get to the base application directory
                var baseAppDir = Path.GetDirectoryName(Path.GetDirectoryName(dataDir)); // Remove 'cl.sqlite', then 'data'
                sqliteConfigPath = Path.Combine(baseAppDir ?? "", "config", "sqlite.json");
            }

            if (!string.IsNullOrEmpty(sqliteConfigPath) && File.Exists(sqliteConfigPath))
            {
                _logger?.Info($"Loading SQLite configuration from file: {sqliteConfigPath}");

                var fileContent = File.ReadAllText(sqliteConfigPath);
                var configData = JObject.Parse(fileContent);

                // Get the default configuration section
                var defaultConfig = configData["Default"] ?? configData["default"];
                if (defaultConfig != null)
                {
                    var config = new SQLiteConfiguration
                    {
                        DatabasePath = defaultConfig["database_path"]?.Value<string>() ?? "database.db",
                        ConnectionTimeoutSeconds = defaultConfig["connection_timeout"]?.Value<uint>() ?? 30,
                        CommandTimeoutSeconds = defaultConfig["command_timeout"]?.Value<uint>() ?? 120,
                        SkipTableSync = defaultConfig["skip_table_sync"]?.Value<bool>() ?? false,
                        UseWAL = defaultConfig["use_wal"]?.Value<bool>() ?? true,
                        EnableForeignKeys = defaultConfig["enable_foreign_keys"]?.Value<bool>() ?? true,
                        MaxPoolSize = defaultConfig["max_pool_size"]?.Value<int>() ?? 10,
                        CacheMode = defaultConfig["cache_mode"]?.Value<string>() switch
                        {
                            "shared" => CacheMode.Shared,
                            "private" => CacheMode.Private,
                            _ => CacheMode.Default
                        }
                    };

                    _logger?.Info($"SQLite configuration loaded from file successfully");
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Failed to load SQLite configuration from file: {ex.Message}");
        }

        return null;
    }

    public Task OnInitializeAsync()
    {
        if (_logger == null || _config == null)
            throw new InvalidOperationException("Library not loaded");

        _logger.Info($"Initializing {Manifest.Name}");

        _connectionManager = new ConnectionManager(_config, _logger, _dataDirectory);
        _tableSyncService = new TableSyncService(_connectionManager, _dataDirectory, _logger);
        _initialized = true;

        _logger.Info($"{Manifest.Name} initialized successfully");
        return Task.CompletedTask;
    }

    public Task OnUnloadAsync()
    {
        _logger?.Info($"Unloading {Manifest.Name}");

        _connectionManager?.Dispose();
        _connectionManager = null;
        _initialized = false;

        _logger?.Info($"{Manifest.Name} unloaded successfully");
        return Task.CompletedTask;
    }

    public Task<HealthCheckResult> HealthCheckAsync()
    {
        if (!_initialized || _connectionManager == null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Library not initialized",
                new InvalidOperationException("Library is not properly initialized")));
        }

        // Try to get a connection as a health check
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var conn = _connectionManager.GetConnectionAsync(cts.Token).GetAwaiter().GetResult();
            _connectionManager.ReleaseConnectionAsync(conn).GetAwaiter().GetResult();

            return Task.FromResult(HealthCheckResult.Healthy($"{Manifest.Name} is operational"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Failed to get database connection",
                ex));
        }
    }

    /// <summary>
    /// Gets the connection manager (must be initialized first)
    /// </summary>
    public ConnectionManager GetConnectionManager()
    {
        if (_connectionManager == null)
            throw new InvalidOperationException("Library not initialized");

        return _connectionManager;
    }

    /// <summary>
    /// Creates a repository for the specified model type
    /// </summary>
    public Repository<T> CreateRepository<T>() where T : class, new()
    {
        if (_connectionManager == null || _logger == null)
            throw new InvalidOperationException("Library not initialized");

        return new Repository<T>(_connectionManager, _logger);
    }
}

/// <summary>
/// Manifest for the SQLite library
/// </summary>
internal class SQLiteManifest : ILibraryManifest
{
    public string Id => "cl.sqlite";
    public string Name => "CL.SQLite";
    public string Version => "2.0.0";
    public string Author => "Media2A.com";
    public string Description => "SQLite database library with model-based operations and connection pooling";

    public IReadOnlyList<LibraryDependency> Dependencies { get; } = new List<LibraryDependency>
    {
        new() { Id = "cl.core", MinVersion = "2.0.0", IsOptional = false }
    };

    public IReadOnlyList<string> Tags { get; } = new List<string>
    {
        "database", "sqlite", "orm", "repository"
    };
}
