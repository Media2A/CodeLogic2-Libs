using CodeLogic.Abstractions;
using CL.MySQL2.Models;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using System.Collections.Concurrent;

namespace CL.MySQL2.Services;

/// <summary>
/// Manages MySQL database connections with optimized pooling and caching.
/// Fully integrated with CodeLogic framework for logging and configuration.
/// </summary>
public class ConnectionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, DatabaseConfiguration> _configurations = new();
    private readonly IMemoryCache _connectionStringCache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _connectionLocks = new();
    private readonly ILogger? _logger;
    private bool _disposed = false;

    public ConnectionManager(ILogger? logger = null)
    {
        _logger = logger;
        _connectionStringCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 100
        });
    }

    /// <summary>
    /// Registers a database configuration for a specific connection ID.
    /// </summary>
    public void RegisterConfiguration(DatabaseConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _configurations[configuration.ConnectionId] = configuration;
        _logger?.Info($"Registered database configuration for connection ID: {configuration.ConnectionId}");
    }

    /// <summary>
    /// Gets a database configuration by connection ID.
    /// </summary>
    public DatabaseConfiguration GetConfiguration(string connectionId = "Default")
    {
        if (_configurations.TryGetValue(connectionId, out var config))
            return config;

        throw new InvalidOperationException($"Configuration for connection ID '{connectionId}' not found. Please register it first using RegisterConfiguration.");
    }

    /// <summary>
    /// Checks if a configuration exists for the given connection ID.
    /// </summary>
    public bool HasConfiguration(string connectionId = "Default")
    {
        return _configurations.ContainsKey(connectionId);
    }

    /// <summary>
    /// Gets a connection string for the specified connection ID.
    /// Results are cached for performance.
    /// </summary>
    public string GetConnectionString(string connectionId = "Default")
    {
        var cacheKey = $"ConnectionString_{connectionId}";

        if (_connectionStringCache.TryGetValue<string>(cacheKey, out var cached))
            return cached!;

        var config = GetConfiguration(connectionId);
        var connectionString = config.BuildConnectionString();

        _connectionStringCache.Set(cacheKey, connectionString, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Size = 1
        });

        return connectionString;
    }

    /// <summary>
    /// Opens a new MySQL connection asynchronously.
    /// </summary>
    public async Task<MySqlConnection> OpenConnectionAsync(string connectionId = "Default", CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = GetConnectionString(connectionId);
            var connection = new MySqlConnection(connectionString);

            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var config = GetConfiguration(connectionId);
            if (config.EnableLogging)
            {
                _logger?.Debug($"Opened connection for ID: {connectionId}");
            }

            return connection;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to open database connection for ID: {connectionId}", ex);
            throw new InvalidOperationException($"Failed to open database connection for ID: {connectionId}", ex);
        }
    }

    /// <summary>
    /// Closes a MySQL connection asynchronously.
    /// </summary>
    public async Task CloseConnectionAsync(MySqlConnection? connection)
    {
        if (connection != null && connection.State != System.Data.ConnectionState.Closed)
        {
            try
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error closing connection: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Tests a database connection.
    /// </summary>
    public async Task<bool> TestConnectionAsync(string connectionId = "Default")
    {
        try
        {
            using var connection = await OpenConnectionAsync(connectionId).ConfigureAwait(false);
            using var command = new MySqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
            var success = result != null && Convert.ToInt32(result) == 1;

            if (success)
            {
                _logger?.Info($"Connection test successful for '{connectionId}'");
            }
            else
            {
                _logger?.Warning($"Connection test returned unexpected result for '{connectionId}'");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Connection test failed for '{connectionId}': {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Executes a function with an automatically managed connection.
    /// </summary>
    public async Task<T> ExecuteWithConnectionAsync<T>(
        Func<MySqlConnection, Task<T>> func,
        string connectionId = "Default",
        CancellationToken cancellationToken = default)
    {
        MySqlConnection? connection = null;
        try
        {
            connection = await OpenConnectionAsync(connectionId, cancellationToken).ConfigureAwait(false);
            return await func(connection).ConfigureAwait(false);
        }
        finally
        {
            if (connection != null)
            {
                await CloseConnectionAsync(connection).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Executes a function with an automatically managed transaction.
    /// </summary>
    public async Task<T> ExecuteWithTransactionAsync<T>(
        Func<MySqlConnection, MySqlTransaction, Task<T>> func,
        string connectionId = "Default",
        CancellationToken cancellationToken = default)
    {
        MySqlConnection? connection = null;
        MySqlTransaction? transaction = null;

        try
        {
            connection = await OpenConnectionAsync(connectionId, cancellationToken).ConfigureAwait(false);
            transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var result = await func(connection, transaction).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            if (transaction != null)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    _logger?.Warning($"Transaction rolled back due to error: {ex.Message}");
                }
                catch (Exception rollbackEx)
                {
                    _logger?.Error($"Error rolling back transaction: {rollbackEx.Message}", rollbackEx);
                }
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
            if (connection != null)
            {
                await CloseConnectionAsync(connection).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Gets information about the database server.
    /// </summary>
    public async Task<(string Version, string ServerInfo)?> GetServerInfoAsync(string connectionId = "Default")
    {
        try
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                using var cmd = new MySqlCommand("SELECT VERSION(), @@version_comment", connection);
                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var version = reader.GetString(0);
                    var serverInfo = reader.GetString(1);
                    return ((string Version, string ServerInfo)?)(version, serverInfo);
                }

                return ((string Version, string ServerInfo)?)null;
            }, connectionId);
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to retrieve server info for '{connectionId}': {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Clears all cached connection strings.
    /// </summary>
    public void ClearCache()
    {
        _connectionStringCache.Dispose();
        _logger?.Info("Connection string cache cleared");
    }

    /// <summary>
    /// Gets all registered connection IDs.
    /// </summary>
    public IEnumerable<string> GetConnectionIds()
    {
        return _configurations.Keys;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _connectionStringCache?.Dispose();

        foreach (var semaphore in _connectionLocks.Values)
        {
            semaphore?.Dispose();
        }
        _connectionLocks.Clear();
        _configurations.Clear();

        _logger?.Info("ConnectionManager disposed");
        _disposed = true;
    }
}
