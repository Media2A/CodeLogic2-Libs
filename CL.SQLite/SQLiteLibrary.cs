using CodeLogic.Abstractions;
using CodeLogic.Models;
using CL.SQLite.Models;
using CL.SQLite.Services;

namespace CL.SQLite;

/// <summary>
/// SQLite library implementation for CodeLogic framework
/// </summary>
public class SQLiteLibrary : ILibrary
{
    private ConnectionManager? _connectionManager;
    private ILogger? _logger;
    private SQLiteConfiguration? _config;
    private bool _initialized;

    public ILibraryManifest Manifest { get; } = new SQLiteManifest();

    public Task OnLoadAsync(LibraryContext context)
    {
        _logger = context.Logger;
        _logger.Info($"Loading {Manifest.Name} v{Manifest.Version}");

        // Get configuration or use defaults
        _config = context.Configuration.TryGetValue("SQLite", out var configObj) && configObj is SQLiteConfiguration sqliteConfig
            ? sqliteConfig
            : new SQLiteConfiguration();

        return Task.CompletedTask;
    }

    public Task OnInitializeAsync()
    {
        if (_logger == null || _config == null)
            throw new InvalidOperationException("Library not loaded");

        _logger.Info($"Initializing {Manifest.Name}");

        _connectionManager = new ConnectionManager(_config, _logger);
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
