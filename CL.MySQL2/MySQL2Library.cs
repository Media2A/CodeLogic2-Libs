using CodeLogic.Abstractions;
using CL.MySQL2.Models;
using CL.MySQL2.Services;
using CodeLogic.Configuration;
using Newtonsoft.Json;

namespace CL.MySQL2;

/// <summary>
/// CL.MySQL2 - A fully integrated MySQL library for the CodeLogic framework.
/// Provides high-performance database operations with comprehensive logging, configuration, and connection management.
/// </summary>
public class MySQL2Library : ILibrary
{
    private LibraryContext? _context;
    private ILogger? _logger;
    private ConnectionManager? _connectionManager;
    private ConfigurationManager? _configManager;
    private readonly Dictionary<string, DatabaseConfiguration> _databaseConfigs = new();

    public ILibraryManifest Manifest { get; } = new MySQL2Manifest();

    public async Task OnLoadAsync(LibraryContext context)
    {
        _context = context;
        _logger = context.Logger as ILogger;

        Console.WriteLine($"    [CL.MySQL2] MySQL2 library loading...");
        Console.WriteLine($"    [CL.MySQL2] Data directory: {context.DataDirectory}");

        // Get configuration manager from services
        _configManager = context.Services.GetService(typeof(ConfigurationManager)) as ConfigurationManager;

        // Initialize connection manager with logger
        _connectionManager = new ConnectionManager(_logger);

        _logger?.Info("CL.MySQL2 library loaded successfully");

        await Task.CompletedTask;
    }

    public async Task OnInitializeAsync()
    {
        Console.WriteLine($"    [CL.MySQL2] Initializing MySQL2 library...");

        if (_context == null || _connectionManager == null)
        {
            Console.WriteLine($"    [CL.MySQL2] ✗ Error: Context or ConnectionManager not initialized!");
            return;
        }

        try
        {
            // Load database configurations from the CodeLogic configuration system
            await LoadDatabaseConfigurationsAsync();

            // Test connections for all registered databases
            await TestDatabaseConnectionsAsync();

            Console.WriteLine($"    [CL.MySQL2] ✓ Initialized successfully with {_databaseConfigs.Count} database(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    [CL.MySQL2] ✗ Initialization failed: {ex.Message}");
            _logger?.Error("Failed to initialize CL.MySQL2", ex);
        }
    }

    public async Task OnUnloadAsync()
    {
        Console.WriteLine($"    [CL.MySQL2] Shutting down MySQL2 library...");

        _connectionManager?.Dispose();

        _logger?.Info("CL.MySQL2 library unloaded");
        Console.WriteLine($"    [CL.MySQL2] Library unloaded successfully");

        await Task.CompletedTask;
    }

    public async Task<HealthCheckResult> HealthCheckAsync()
    {
        if (_context == null || _connectionManager == null)
        {
            return HealthCheckResult.Unhealthy("Library not initialized");
        }

        // Test all database connections
        var connectionIds = _connectionManager.GetConnectionIds().ToList();

        if (!connectionIds.Any())
        {
            return HealthCheckResult.Unhealthy("No database connections configured");
        }

        var failedConnections = new List<string>();

        foreach (var connectionId in connectionIds)
        {
            var testResult = await _connectionManager.TestConnectionAsync(connectionId);
            if (!testResult)
            {
                failedConnections.Add(connectionId);
            }
        }

        if (failedConnections.Any())
        {
            return HealthCheckResult.Unhealthy(
                $"Failed to connect to database(s): {string.Join(", ", failedConnections)}");
        }

        return HealthCheckResult.Healthy($"All {connectionIds.Count} database connection(s) are operational");
    }

    /// <summary>
    /// Gets a repository for the specified model type.
    /// </summary>
    public Repository<T>? GetRepository<T>(string connectionId = "Default") where T : class, new()
    {
        if (_connectionManager == null)
        {
            _logger?.Error("Cannot create repository: ConnectionManager not initialized");
            return null;
        }

        try
        {
            return new Repository<T>(_connectionManager, _logger, connectionId);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to create repository for {typeof(T).Name}", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets a query builder for the specified model type.
    /// </summary>
    public QueryBuilder<T>? GetQueryBuilder<T>(string connectionId = "Default") where T : class, new()
    {
        if (_connectionManager == null)
        {
            _logger?.Error("Cannot create query builder: ConnectionManager not initialized");
            return null;
        }

        try
        {
            return new QueryBuilder<T>(_connectionManager, _logger, connectionId);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to create query builder for {typeof(T).Name}", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets a non-generic query builder factory.
    /// </summary>
    public QueryBuilder? GetQueryBuilder(string connectionId = "Default")
    {
        if (_connectionManager == null)
        {
            _logger?.Error("Cannot create query builder: ConnectionManager not initialized");
            return null;
        }

        try
        {
            return new QueryBuilder(_connectionManager, _logger, connectionId);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to create query builder", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets the connection manager instance.
    /// </summary>
    public ConnectionManager? GetConnectionManager()
    {
        return _connectionManager;
    }

    /// <summary>
    /// Registers a new database configuration at runtime.
    /// </summary>
    public void RegisterDatabase(string connectionId, DatabaseConfiguration config)
    {
        if (_connectionManager == null)
        {
            throw new InvalidOperationException("ConnectionManager not initialized");
        }

        config.ConnectionId = connectionId;
        _connectionManager.RegisterConfiguration(config);
        _databaseConfigs[connectionId] = config;

        _logger?.Info($"Registered database configuration: {connectionId}");
    }

    private async Task LoadDatabaseConfigurationsAsync()
    {
        Console.WriteLine($"    [CL.MySQL2] LoadDatabaseConfigurationsAsync: _context?.DataDirectory = '{_context?.DataDirectory}'");

        // Try to load the mysql.json config file directly from global config subdirectory
        // The config files are stored in the global 'config' directory, not in the library's data directory
        string mysqlConfigPath = "";

        // First, try to construct path from DataDirectory (which is like bin/Debug/net10.0/data/cl.mysql2)
        // and go up to find the global config directory
        if (!string.IsNullOrEmpty(_context?.DataDirectory))
        {
            var dataDir = _context.DataDirectory;
            // Remove the 'data/cl.mysql2' part to get to the base application directory
            var baseAppDir = Path.GetDirectoryName(Path.GetDirectoryName(dataDir)); // Remove 'cl.mysql2', then 'data'
            mysqlConfigPath = Path.Combine(baseAppDir ?? "", "config", "mysql.json");
        }

        Console.WriteLine($"    [CL.MySQL2] LoadDatabaseConfigurationsAsync: mysqlConfigPath = '{mysqlConfigPath}'");
        Console.WriteLine($"    [CL.MySQL2] LoadDatabaseConfigurationsAsync: File.Exists = {File.Exists(mysqlConfigPath)}");

        if (!string.IsNullOrEmpty(mysqlConfigPath) && File.Exists(mysqlConfigPath))
        {
            Console.WriteLine($"    [CL.MySQL2] File exists, attempting to load...");
            _logger?.Info($"Loading MySQL configuration from file: {mysqlConfigPath}");

            try
            {
                var fileContent = await File.ReadAllTextAsync(mysqlConfigPath);
                Console.WriteLine($"    [CL.MySQL2] File content length: {fileContent.Length} bytes");

                var configData = JsonConvert.DeserializeObject<Dictionary<string, object>>(fileContent);
                Console.WriteLine($"    [CL.MySQL2] Deserialized config count: {configData?.Count ?? 0}");

                if (configData != null && configData.Count > 0)
                {
                    _logger?.Info($"Loaded mysql.json with {configData.Count} configuration(s)");

                    // Try to load as multi-database config (check for known database connection IDs)
                    var knownConnectionIds = new[] { "Default", "Analytics", "Reporting", "Archive", "Staging" };
                    var loadedConnections = new List<string>();

                    foreach (var connectionId in knownConnectionIds)
                    {
                        Console.WriteLine($"    [CL.MySQL2] Checking for connection '{connectionId}': Contains={configData.ContainsKey(connectionId)}");

                        if (configData.ContainsKey(connectionId))
                        {
                            var configValue = configData[connectionId];
                            Console.WriteLine($"    [CL.MySQL2] Value type for '{connectionId}': {configValue?.GetType().FullName}");

                            // Convert JObject to Dictionary if needed
                            Dictionary<string, object>? dbConfigDict = null;
                            if (configValue is Dictionary<string, object> dict)
                            {
                                dbConfigDict = dict;
                            }
                            else if (configValue != null)
                            {
                                // Try to convert from JObject or other dynamic types
                                try
                                {
                                    dbConfigDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(configValue));
                                }
                                catch
                                {
                                    // Conversion failed, skip this connection
                                }
                            }

                            if (dbConfigDict != null)
                            {
                                Console.WriteLine($"    [CL.MySQL2] Loading connection '{connectionId}'");

                            // This is a multi-database config where connectionId is a sub-key with nested properties
                            var host = dbConfigDict.ContainsKey("host") ? dbConfigDict["host"]?.ToString() ?? "localhost" : "localhost";
                            var port = dbConfigDict.ContainsKey("port") ? int.Parse(dbConfigDict["port"]?.ToString() ?? "3306") : 3306;
                            var dbName = dbConfigDict.ContainsKey("database") ? dbConfigDict["database"]?.ToString() ?? "" : "";
                            var username = dbConfigDict.ContainsKey("username") ? dbConfigDict["username"]?.ToString() ?? "" : "";
                            var password = dbConfigDict.ContainsKey("password") ? dbConfigDict["password"]?.ToString() ?? "" : "";
                            var minPoolSize = dbConfigDict.ContainsKey("min_pool_size") ? int.Parse(dbConfigDict["min_pool_size"]?.ToString() ?? "5") : 5;
                            var maxPoolSize = dbConfigDict.ContainsKey("max_pool_size") ? int.Parse(dbConfigDict["max_pool_size"]?.ToString() ?? "100") : 100;
                            var connectionTimeout = dbConfigDict.ContainsKey("connection_timeout") ? int.Parse(dbConfigDict["connection_timeout"]?.ToString() ?? "30") : 30;
                            var enableLogging = dbConfigDict.ContainsKey("enable_logging") ? bool.Parse(dbConfigDict["enable_logging"]?.ToString() ?? "false") : false;

                            Console.WriteLine($"    [CL.MySQL2] Parsed config - host={host}, port={port}, database={dbName}");

                            if (!string.IsNullOrEmpty(dbName))
                            {
                                var dbConfig = new DatabaseConfiguration
                                {
                                    ConnectionId = connectionId,
                                    Host = host,
                                    Port = port,
                                    Database = dbName,
                                    Username = username,
                                    Password = password,
                                    MinPoolSize = minPoolSize,
                                    MaxPoolSize = maxPoolSize,
                                    ConnectionTimeout = connectionTimeout,
                                    EnableLogging = enableLogging
                                };

                                RegisterDatabase(connectionId, dbConfig);
                                loadedConnections.Add(connectionId);
                                _logger?.Info($"Loaded database configuration: {connectionId} -> {dbName}");
                            }
                            }
                        }
                    }

                    if (loadedConnections.Count > 0)
                    {
                        _logger?.Info($"Loaded {loadedConnections.Count} database configuration(s) from mysql.json: {string.Join(", ", loadedConnections)}");
                    }
                }
                else
                {
                    Console.WriteLine($"    [CL.MySQL2] WARNING: configData is null or empty!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    [CL.MySQL2] Exception during file load: {ex.GetType().Name}: {ex.Message}");
                _logger?.Warning($"Failed to load MySQL configuration from file: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"    [CL.MySQL2] File does not exist, will generate default template");

            // Generate default configuration file if none exists
            _logger?.Info("No MySQL configuration found, creating default multi-database configuration template...");

            if (_configManager != null)
            {
                // Create multi-database template with examples
                var defaultConfig = new Dictionary<string, object>
                {
                    ["Default"] = new Dictionary<string, object>
                    {
                        ["host"] = "localhost",
                        ["port"] = 3306,
                        ["database"] = "main_database",
                        ["username"] = "root",
                        ["password"] = "",
                        ["min_pool_size"] = 5,
                        ["max_pool_size"] = 100,
                        ["connection_timeout"] = 30,
                        ["enable_logging"] = false
                    },
                    ["Analytics"] = new Dictionary<string, object>
                    {
                        ["host"] = "localhost",
                        ["port"] = 3306,
                        ["database"] = "analytics_db",
                        ["username"] = "root",
                        ["password"] = "",
                        ["min_pool_size"] = 3,
                        ["max_pool_size"] = 50,
                        ["connection_timeout"] = 30,
                        ["enable_logging"] = false
                    }
                };

                await _configManager.GenerateDefaultAsync("mysql", defaultConfig);
                Console.WriteLine($"    [CL.MySQL2] Created default multi-database configuration template at config/mysql.json");
                Console.WriteLine($"    [CL.MySQL2] Configured with example databases: Default, Analytics");

                // Now try to load the databases we just created
                _logger?.Info("Attempting to load generated multi-database configurations...");
                var loadedConnections = new List<string>();
                var knownConnectionIds = new[] { "Default", "Analytics", "Reporting", "Archive", "Staging" };

                foreach (var connectionId in knownConnectionIds)
                {
                    var configObject = defaultConfig.ContainsKey(connectionId) ? defaultConfig[connectionId] : null;

                    if (configObject is Dictionary<string, object> dbConfigDict)
                    {
                        var host = dbConfigDict.ContainsKey("host") ? dbConfigDict["host"]?.ToString() ?? "localhost" : "localhost";
                        var port = dbConfigDict.ContainsKey("port") ? int.Parse(dbConfigDict["port"]?.ToString() ?? "3306") : 3306;
                        var dbName = dbConfigDict.ContainsKey("database") ? dbConfigDict["database"]?.ToString() ?? "" : "";
                        var username = dbConfigDict.ContainsKey("username") ? dbConfigDict["username"]?.ToString() ?? "" : "";
                        var password = dbConfigDict.ContainsKey("password") ? dbConfigDict["password"]?.ToString() ?? "" : "";
                        var minPoolSize = dbConfigDict.ContainsKey("min_pool_size") ? int.Parse(dbConfigDict["min_pool_size"]?.ToString() ?? "5") : 5;
                        var maxPoolSize = dbConfigDict.ContainsKey("max_pool_size") ? int.Parse(dbConfigDict["max_pool_size"]?.ToString() ?? "100") : 100;
                        var connectionTimeout = dbConfigDict.ContainsKey("connection_timeout") ? int.Parse(dbConfigDict["connection_timeout"]?.ToString() ?? "30") : 30;
                        var enableLogging = dbConfigDict.ContainsKey("enable_logging") ? bool.Parse(dbConfigDict["enable_logging"]?.ToString() ?? "false") : false;

                        if (!string.IsNullOrEmpty(dbName))
                        {
                            var dbConfig = new DatabaseConfiguration
                            {
                                ConnectionId = connectionId,
                                Host = host,
                                Port = port,
                                Database = dbName,
                                Username = username,
                                Password = password,
                                MinPoolSize = minPoolSize,
                                MaxPoolSize = maxPoolSize,
                                ConnectionTimeout = connectionTimeout,
                                EnableLogging = enableLogging
                            };

                            RegisterDatabase(connectionId, dbConfig);
                            loadedConnections.Add(connectionId);
                            _logger?.Info($"Loaded template database configuration: {connectionId} -> {dbName}");
                        }
                    }
                }

                if (loadedConnections.Count > 0)
                {
                    _logger?.Info($"Successfully loaded {loadedConnections.Count} database configuration(s) from template: {string.Join(", ", loadedConnections)}");
                }
            }
        }

        await Task.CompletedTask;
    }

    private async Task TestDatabaseConnectionsAsync()
    {
        if (_connectionManager == null)
            return;

        var connectionIds = _connectionManager.GetConnectionIds().ToList();

        foreach (var connectionId in connectionIds)
        {
            Console.Write($"    [CL.MySQL2] Testing connection '{connectionId}'... ");

            var success = await _connectionManager.TestConnectionAsync(connectionId);

            if (success)
            {
                Console.WriteLine("✓ Connected");

                // Get server info
                var serverInfo = await _connectionManager.GetServerInfoAsync(connectionId);
                if (serverInfo.HasValue)
                {
                    Console.WriteLine($"    [CL.MySQL2]   Server: {serverInfo.Value.ServerInfo} v{serverInfo.Value.Version}");
                }
            }
            else
            {
                Console.WriteLine("✗ Failed");
                _logger?.Warning($"Failed to connect to database '{connectionId}'");
            }
        }
    }
}

/// <summary>
/// Manifest for CL.MySQL2 library
/// </summary>
public class MySQL2Manifest : ILibraryManifest
{
    public string Id => "cl.mysql2";
    public string Name => "CL.MySQL2";
    public string Version => "2.0.0";
    public string Author => "Media2A";
    public string Description => "Fully integrated MySQL database library with connection pooling, caching, and comprehensive ORM support";

    public IReadOnlyList<LibraryDependency> Dependencies { get; } = Array.Empty<LibraryDependency>();

    public IReadOnlyList<string> Tags { get; } = new[] { "database", "mysql", "orm", "repository" };
}
