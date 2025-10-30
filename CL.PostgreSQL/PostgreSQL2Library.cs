using CodeLogic.Abstractions;
using CL.PostgreSQL.Models;
using CL.PostgreSQL.Services;
using CodeLogic.Configuration;
using Newtonsoft.Json;

namespace CL.PostgreSQL;

/// <summary>
/// CL.PostgreSQL - A fully integrated PostgreSQL library for the CodeLogic framework.
/// Provides high-performance database operations with comprehensive logging, configuration, and connection management.
/// </summary>
public class PostgreSQL2Library : ILibrary
{
    private LibraryContext? _context;
    private ILogger? _logger;
    private ConnectionManager? _connectionManager;
    private ConfigurationManager? _configManager;
    private TableSyncService? _tableSyncService;
    private readonly Dictionary<string, DatabaseConfiguration> _databaseConfigs = new();

    public ILibraryManifest Manifest { get; } = new PostgreSQL2Manifest();

    public async Task OnLoadAsync(LibraryContext context)
    {
        _context = context;
        _logger = context.Logger as ILogger;

        Console.WriteLine($"    [CL.PostgreSQL] PostgreSQL library loading...");
        Console.WriteLine($"    [CL.PostgreSQL] Data directory: {context.DataDirectory}");

        // Get configuration manager from services
        _configManager = context.Services.GetService(typeof(ConfigurationManager)) as ConfigurationManager;

        // Initialize connection manager with logger
        _connectionManager = new ConnectionManager(_logger);

        _logger?.Info("CL.PostgreSQL library loaded successfully");

        await Task.CompletedTask;
    }

    public async Task OnInitializeAsync()
    {
        Console.WriteLine($"    [CL.PostgreSQL] Initializing PostgreSQL library...");

        if (_context == null || _connectionManager == null)
        {
            Console.WriteLine($"    [CL.PostgreSQL] ✗ Error: Context or ConnectionManager not initialized!");
            return;
        }

        try
        {
            // Load database configurations from the CodeLogic configuration system
            await LoadDatabaseConfigurationsAsync();

            // Test connections for all registered databases
            await TestDatabaseConnectionsAsync();

            // Initialize TableSyncService
            if (!string.IsNullOrEmpty(_context.DataDirectory))
            {
                _tableSyncService = new TableSyncService(_connectionManager, _context.DataDirectory, _logger);
                _logger?.Info("TableSyncService initialized successfully");
            }

            Console.WriteLine($"    [CL.PostgreSQL] ✓ Initialized successfully with {_databaseConfigs.Count} database(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    [CL.PostgreSQL] ✗ Initialization failed: {ex.Message}");
            _logger?.Error("Failed to initialize CL.PostgreSQL", ex);
        }
    }

    public async Task OnUnloadAsync()
    {
        Console.WriteLine($"    [CL.PostgreSQL] Shutting down PostgreSQL library...");

        _connectionManager?.Dispose();

        _logger?.Info("CL.PostgreSQL library unloaded");
        Console.WriteLine($"    [CL.PostgreSQL] Library unloaded successfully");

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
            return new QueryBuilder(_connectionManager, _logger);
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
    /// Gets the table sync service for schema synchronization.
    /// </summary>
    public TableSyncService? GetTableSyncService()
    {
        return _tableSyncService;
    }

    /// <summary>
    /// Synchronizes a single model type with its database table.
    /// </summary>
    public async Task<bool> SyncTableAsync<T>(
        string connectionId = "Default",
        bool createBackup = true) where T : class
    {
        if (_tableSyncService == null)
        {
            _logger?.Error("Cannot sync table: TableSyncService not initialized");
            return false;
        }

        try
        {
            return await _tableSyncService.SyncTableAsync<T>(connectionId, createBackup);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to sync table: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Synchronizes multiple model types at once.
    /// </summary>
    public async Task<Dictionary<string, bool>> SyncTablesAsync(
        Type[] modelTypes,
        string connectionId = "Default",
        bool createBackup = true)
    {
        if (_tableSyncService == null)
        {
            _logger?.Error("Cannot sync tables: TableSyncService not initialized");
            return new Dictionary<string, bool>();
        }

        try
        {
            return await _tableSyncService.SyncTablesAsync(modelTypes, connectionId, createBackup);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to sync tables: {ex.Message}", ex);
            return new Dictionary<string, bool>();
        }
    }

    /// <summary>
    /// Synchronizes all model types in a specified namespace.
    /// </summary>
    public async Task<Dictionary<string, bool>> SyncNamespaceAsync(
        string namespaceName,
        string connectionId = "Default",
        bool createBackup = true,
        bool includeDerivedNamespaces = false)
    {
        if (_tableSyncService == null)
        {
            _logger?.Error("Cannot sync namespace: TableSyncService not initialized");
            return new Dictionary<string, bool>();
        }

        try
        {
            return await _tableSyncService.SyncNamespaceAsync(
                namespaceName,
                connectionId,
                createBackup,
                includeDerivedNamespaces);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to sync namespace: {ex.Message}", ex);
            return new Dictionary<string, bool>();
        }
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
        Console.WriteLine($"    [CL.PostgreSQL] LoadDatabaseConfigurationsAsync: _context?.DataDirectory = '{_context?.DataDirectory}'");

        string postgresqlConfigPath = "";

        // First, try to construct path from DataDirectory (which is like bin/Debug/net10.0/data/cl.postgresql)
        // and go up to find the global config directory
        if (!string.IsNullOrEmpty(_context?.DataDirectory))
        {
            var dataDir = _context.DataDirectory;
            // Remove the 'data/cl.postgresql' part to get to the base application directory
            var baseAppDir = Path.GetDirectoryName(Path.GetDirectoryName(dataDir)); // Remove 'cl.postgresql', then 'data'
            postgresqlConfigPath = Path.Combine(baseAppDir ?? "", "config", "postgresql.json");
        }

        Console.WriteLine($"    [CL.PostgreSQL] LoadDatabaseConfigurationsAsync: postgresqlConfigPath = '{postgresqlConfigPath}'");
        Console.WriteLine($"    [CL.PostgreSQL] LoadDatabaseConfigurationsAsync: File.Exists = {File.Exists(postgresqlConfigPath)}");

        if (!string.IsNullOrEmpty(postgresqlConfigPath) && File.Exists(postgresqlConfigPath))
        {
            Console.WriteLine($"    [CL.PostgreSQL] File exists, attempting to load...");
            _logger?.Info($"Loading PostgreSQL configuration from file: {postgresqlConfigPath}");

            try
            {
                var fileContent = await File.ReadAllTextAsync(postgresqlConfigPath);
                Console.WriteLine($"    [CL.PostgreSQL] File content length: {fileContent.Length} bytes");

                var configData = JsonConvert.DeserializeObject<Dictionary<string, object>>(fileContent);
                Console.WriteLine($"    [CL.PostgreSQL] Deserialized config count: {configData?.Count ?? 0}");

                if (configData != null && configData.Count > 0)
                {
                    _logger?.Info($"Loaded postgresql.json with {configData.Count} configuration(s)");

                    // Try to load as multi-database config (check for known database connection IDs)
                    var knownConnectionIds = new[] { "Default", "Demo", "Analytics", "Reporting", "Archive", "Staging" };
                    var loadedConnections = new List<string>();

                    foreach (var connectionId in knownConnectionIds)
                    {
                        Console.WriteLine($"    [CL.PostgreSQL] Checking for connection '{connectionId}': Contains={configData.ContainsKey(connectionId)}");

                        if (configData.ContainsKey(connectionId))
                        {
                            var configValue = configData[connectionId];
                            Console.WriteLine($"    [CL.PostgreSQL] Value type for '{connectionId}': {configValue?.GetType().FullName}");

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
                                // Check if connection is enabled (default: true)
                                var enabled = dbConfigDict.ContainsKey("enabled") ? bool.Parse(dbConfigDict["enabled"]?.ToString() ?? "true") : true;

                                if (!enabled)
                                {
                                    Console.WriteLine($"    [CL.PostgreSQL] Skipping disabled connection '{connectionId}'");
                                    _logger?.Debug($"Skipping disabled database configuration: {connectionId}");
                                    continue;
                                }

                                Console.WriteLine($"    [CL.PostgreSQL] Loading connection '{connectionId}'");

                                var host = dbConfigDict.ContainsKey("host") ? dbConfigDict["host"]?.ToString() ?? "localhost" : "localhost";
                                var port = dbConfigDict.ContainsKey("port") ? int.Parse(dbConfigDict["port"]?.ToString() ?? "5432") : 5432;
                                var dbName = dbConfigDict.ContainsKey("database") ? dbConfigDict["database"]?.ToString() ?? "" : "";
                                var username = dbConfigDict.ContainsKey("username") ? dbConfigDict["username"]?.ToString() ?? "" : "";
                                var password = dbConfigDict.ContainsKey("password") ? dbConfigDict["password"]?.ToString() ?? "" : "";
                                var minPoolSize = dbConfigDict.ContainsKey("min_pool_size") ? int.Parse(dbConfigDict["min_pool_size"]?.ToString() ?? "5") : 5;
                                var maxPoolSize = dbConfigDict.ContainsKey("max_pool_size") ? int.Parse(dbConfigDict["max_pool_size"]?.ToString() ?? "100") : 100;
                                var maxIdleTime = dbConfigDict.ContainsKey("max_idle_time") ? int.Parse(dbConfigDict["max_idle_time"]?.ToString() ?? "60") : 60;
                                var connectionTimeout = dbConfigDict.ContainsKey("connection_timeout") ? int.Parse(dbConfigDict["connection_timeout"]?.ToString() ?? "30") : 30;
                                var commandTimeout = dbConfigDict.ContainsKey("command_timeout") ? int.Parse(dbConfigDict["command_timeout"]?.ToString() ?? "30") : 30;
                                var sslModeStr = dbConfigDict.ContainsKey("ssl_mode") ? dbConfigDict["ssl_mode"]?.ToString() ?? "Prefer" : "Prefer";
                                var enableLogging = dbConfigDict.ContainsKey("enable_logging") ? bool.Parse(dbConfigDict["enable_logging"]?.ToString() ?? "false") : false;
                                var enableCaching = dbConfigDict.ContainsKey("enable_caching") ? bool.Parse(dbConfigDict["enable_caching"]?.ToString() ?? "true") : true;
                                var defaultCacheTtl = dbConfigDict.ContainsKey("default_cache_ttl") ? int.Parse(dbConfigDict["default_cache_ttl"]?.ToString() ?? "300") : 300;
                                var enableAutoSync = dbConfigDict.ContainsKey("enable_auto_sync") ? bool.Parse(dbConfigDict["enable_auto_sync"]?.ToString() ?? "true") : true;
                                var logSlowQueries = dbConfigDict.ContainsKey("log_slow_queries") ? bool.Parse(dbConfigDict["log_slow_queries"]?.ToString() ?? "true") : true;
                                var slowQueryThreshold = dbConfigDict.ContainsKey("slow_query_threshold") ? int.Parse(dbConfigDict["slow_query_threshold"]?.ToString() ?? "1000") : 1000;

                                // Parse SSL mode
                                var sslMode = Enum.TryParse<SslMode>(sslModeStr, true, out var parsedSslMode) ? parsedSslMode : SslMode.Prefer;

                                Console.WriteLine($"    [CL.PostgreSQL] Parsed config - host={host}, port={port}, database={dbName}");

                                if (!string.IsNullOrEmpty(dbName))
                                {
                                    var dbConfig = new DatabaseConfiguration
                                    {
                                        ConnectionId = connectionId,
                                        Enabled = enabled,
                                        Host = host,
                                        Port = port,
                                        Database = dbName,
                                        Username = username,
                                        Password = password,
                                        MinPoolSize = minPoolSize,
                                        MaxPoolSize = maxPoolSize,
                                        MaxIdleTime = maxIdleTime,
                                        ConnectionTimeout = connectionTimeout,
                                        CommandTimeout = commandTimeout,
                                        SslMode = sslMode,
                                        EnableLogging = enableLogging,
                                        EnableCaching = enableCaching,
                                        DefaultCacheTtl = defaultCacheTtl,
                                        EnableAutoSync = enableAutoSync,
                                        LogSlowQueries = logSlowQueries,
                                        SlowQueryThreshold = slowQueryThreshold
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
                        _logger?.Info($"Loaded {loadedConnections.Count} database configuration(s) from postgresql.json: {string.Join(", ", loadedConnections)}");
                    }
                }
                else
                {
                    Console.WriteLine($"    [CL.PostgreSQL] WARNING: configData is null or empty!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    [CL.PostgreSQL] Exception during file load: {ex.GetType().Name}: {ex.Message}");
                _logger?.Warning($"Failed to load PostgreSQL configuration from file: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"    [CL.PostgreSQL] File does not exist, will generate default template");

            // Generate default configuration file if none exists
            _logger?.Info("No PostgreSQL configuration found, creating default multi-database configuration template...");

            if (_configManager != null)
            {
                // Create multi-database template with comprehensive feature showcase
                var defaultConfig = new Dictionary<string, object>
                {
                    ["Default"] = new Dictionary<string, object>
                    {
                        ["enabled"] = true,
                        ["host"] = "localhost",
                        ["port"] = 5432,
                        ["database"] = "main_database",
                        ["username"] = "postgres",
                        ["password"] = "",
                        ["min_pool_size"] = 5,
                        ["max_pool_size"] = 100,
                        ["max_idle_time"] = 60,
                        ["connection_timeout"] = 30,
                        ["command_timeout"] = 30,
                        ["ssl_mode"] = "Prefer",
                        ["enable_logging"] = true,
                        ["enable_caching"] = true,
                        ["default_cache_ttl"] = 300,
                        ["enable_auto_sync"] = true,
                        ["log_slow_queries"] = true,
                        ["slow_query_threshold"] = 1000
                    },
                    ["Demo"] = new Dictionary<string, object>
                    {
                        ["enabled"] = true,
                        ["host"] = "localhost",
                        ["port"] = 5432,
                        ["database"] = "demo_database",
                        ["username"] = "postgres",
                        ["password"] = "",
                        ["min_pool_size"] = 5,
                        ["max_pool_size"] = 100,
                        ["max_idle_time"] = 60,
                        ["connection_timeout"] = 30,
                        ["command_timeout"] = 30,
                        ["ssl_mode"] = "Disable",
                        ["enable_logging"] = true,
                        ["enable_caching"] = true,
                        ["default_cache_ttl"] = 300,
                        ["enable_auto_sync"] = true,
                        ["log_slow_queries"] = true,
                        ["slow_query_threshold"] = 500
                    }
                };

                await _configManager.GenerateDefaultAsync("postgresql", defaultConfig);
                Console.WriteLine($"    [CL.PostgreSQL] Created default multi-database configuration template at config/postgresql.json");
                Console.WriteLine($"    [CL.PostgreSQL] Configured with example databases: Default, Demo");

                // Now try to load the databases we just created
                _logger?.Info("Attempting to load generated multi-database configurations...");
                var loadedConnections = new List<string>();
                var knownConnectionIds = new[] { "Default", "Demo", "Analytics", "Reporting", "Archive", "Staging" };

                foreach (var connectionId in knownConnectionIds)
                {
                    var configObject = defaultConfig.ContainsKey(connectionId) ? defaultConfig[connectionId] : null;

                    if (configObject is Dictionary<string, object> dbConfigDict)
                    {
                        // Check if connection is enabled (default: true)
                        var enabled = dbConfigDict.ContainsKey("enabled") ? bool.Parse(dbConfigDict["enabled"]?.ToString() ?? "true") : true;

                        if (!enabled)
                        {
                            _logger?.Debug($"Skipping disabled database configuration: {connectionId}");
                            continue;
                        }

                        var host = dbConfigDict.ContainsKey("host") ? dbConfigDict["host"]?.ToString() ?? "localhost" : "localhost";
                        var port = dbConfigDict.ContainsKey("port") ? int.Parse(dbConfigDict["port"]?.ToString() ?? "5432") : 5432;
                        var dbName = dbConfigDict.ContainsKey("database") ? dbConfigDict["database"]?.ToString() ?? "" : "";
                        var username = dbConfigDict.ContainsKey("username") ? dbConfigDict["username"]?.ToString() ?? "" : "";
                        var password = dbConfigDict.ContainsKey("password") ? dbConfigDict["password"]?.ToString() ?? "" : "";
                        var minPoolSize = dbConfigDict.ContainsKey("min_pool_size") ? int.Parse(dbConfigDict["min_pool_size"]?.ToString() ?? "5") : 5;
                        var maxPoolSize = dbConfigDict.ContainsKey("max_pool_size") ? int.Parse(dbConfigDict["max_pool_size"]?.ToString() ?? "100") : 100;
                        var maxIdleTime = dbConfigDict.ContainsKey("max_idle_time") ? int.Parse(dbConfigDict["max_idle_time"]?.ToString() ?? "60") : 60;
                        var connectionTimeout = dbConfigDict.ContainsKey("connection_timeout") ? int.Parse(dbConfigDict["connection_timeout"]?.ToString() ?? "30") : 30;
                        var commandTimeout = dbConfigDict.ContainsKey("command_timeout") ? int.Parse(dbConfigDict["command_timeout"]?.ToString() ?? "30") : 30;
                        var sslModeStr = dbConfigDict.ContainsKey("ssl_mode") ? dbConfigDict["ssl_mode"]?.ToString() ?? "Prefer" : "Prefer";
                        var enableLogging = dbConfigDict.ContainsKey("enable_logging") ? bool.Parse(dbConfigDict["enable_logging"]?.ToString() ?? "false") : false;
                        var enableCaching = dbConfigDict.ContainsKey("enable_caching") ? bool.Parse(dbConfigDict["enable_caching"]?.ToString() ?? "true") : true;
                        var defaultCacheTtl = dbConfigDict.ContainsKey("default_cache_ttl") ? int.Parse(dbConfigDict["default_cache_ttl"]?.ToString() ?? "300") : 300;
                        var enableAutoSync = dbConfigDict.ContainsKey("enable_auto_sync") ? bool.Parse(dbConfigDict["enable_auto_sync"]?.ToString() ?? "true") : true;
                        var logSlowQueries = dbConfigDict.ContainsKey("log_slow_queries") ? bool.Parse(dbConfigDict["log_slow_queries"]?.ToString() ?? "true") : true;
                        var slowQueryThreshold = dbConfigDict.ContainsKey("slow_query_threshold") ? int.Parse(dbConfigDict["slow_query_threshold"]?.ToString() ?? "1000") : 1000;

                        // Parse SSL mode
                        var sslMode = Enum.TryParse<SslMode>(sslModeStr, true, out var parsedSslMode) ? parsedSslMode : SslMode.Prefer;

                        if (!string.IsNullOrEmpty(dbName))
                        {
                            var dbConfig = new DatabaseConfiguration
                            {
                                ConnectionId = connectionId,
                                Enabled = enabled,
                                Host = host,
                                Port = port,
                                Database = dbName,
                                Username = username,
                                Password = password,
                                MinPoolSize = minPoolSize,
                                MaxPoolSize = maxPoolSize,
                                MaxIdleTime = maxIdleTime,
                                ConnectionTimeout = connectionTimeout,
                                CommandTimeout = commandTimeout,
                                SslMode = sslMode,
                                EnableLogging = enableLogging,
                                EnableCaching = enableCaching,
                                DefaultCacheTtl = defaultCacheTtl,
                                EnableAutoSync = enableAutoSync,
                                LogSlowQueries = logSlowQueries,
                                SlowQueryThreshold = slowQueryThreshold
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
            Console.Write($"    [CL.PostgreSQL] Testing connection '{connectionId}'... ");

            var success = await _connectionManager.TestConnectionAsync(connectionId);

            if (success)
            {
                Console.WriteLine("✓ Connected");

                // Get server info
                var serverInfo = await _connectionManager.GetServerInfoAsync(connectionId);
                if (serverInfo.HasValue)
                {
                    Console.WriteLine($"    [CL.PostgreSQL]   Server: {serverInfo.Value.ServerInfo} v{serverInfo.Value.Version}");
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
/// Manifest for CL.PostgreSQL library
/// </summary>
public class PostgreSQL2Manifest : ILibraryManifest
{
    public string Id => "cl.postgresql";
    public string Name => "CL.PostgreSQL";
    public string Version => "2.0.0";
    public string Author => "Media2A";
    public string Description => "Fully integrated PostgreSQL database library with connection pooling, caching, and comprehensive ORM support";

    public IReadOnlyList<LibraryDependency> Dependencies { get; } = Array.Empty<LibraryDependency>();

    public IReadOnlyList<string> Tags { get; } = new[] { "database", "postgresql", "orm", "repository" };
}
