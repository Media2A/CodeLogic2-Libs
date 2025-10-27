using System.Collections.Concurrent;
using System.Data;
using Microsoft.Data.Sqlite;
using CL.SQLite.Models;
using CodeLogic.Abstractions;

namespace CL.SQLite.Services;

/// <summary>
/// Manages SQLite database connections with connection pooling
/// </summary>
public class ConnectionManager : IDisposable
{
    private readonly SQLiteConfiguration _config;
    private readonly ILogger _logger;
    private readonly ConcurrentStack<PooledConnection> _pool = new();
    private readonly SemaphoreSlim _poolLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public ConnectionManager(SQLiteConfiguration config, ILogger logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        StartCleanupTask();
    }

    /// <summary>
    /// Gets a connection from the pool or creates a new one
    /// </summary>
    public async Task<SqliteConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // Try to get a connection from the pool
        while (_pool.TryPop(out var pooledConn))
        {
            if (pooledConn.IsValid())
            {
                pooledConn.MarkInUse();
                await pooledConn.Lock.WaitAsync(cancellationToken);
                _logger.Trace("Reused pooled connection");
                return pooledConn.Connection;
            }

            // Connection is invalid, dispose it
            pooledConn.Dispose();
        }

        // Need to create a new connection
        await _poolLock.WaitAsync(cancellationToken);
        try
        {
            var connString = BuildConnectionString();
            var connection = new SqliteConnection(connString);
            await connection.OpenAsync(cancellationToken);

            // Configure connection
            await ConfigureConnectionAsync(connection, cancellationToken);

            var pooled = new PooledConnection(connection);
            pooled.MarkInUse();
            await pooled.Lock.WaitAsync(cancellationToken);

            _logger.Trace($"Created new connection: {_config.DatabasePath}");
            return connection;
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// Releases a connection back to the pool
    /// </summary>
    public Task ReleaseConnectionAsync(SqliteConnection connection)
    {
        ThrowIfDisposed();

        if (connection == null)
            return Task.CompletedTask;

        var pooled = new PooledConnection(connection);
        pooled.MarkAvailable();
        pooled.Lock.Release();

        if (_pool.Count < _config.MaxPoolSize)
        {
            _pool.Push(pooled);
            _logger.Trace("Connection returned to pool");
        }
        else
        {
            pooled.Dispose();
            _logger.Trace("Connection disposed (pool full)");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes an action with a connection from the pool
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<SqliteConnection, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        try
        {
            return await action(connection);
        }
        finally
        {
            await ReleaseConnectionAsync(connection);
        }
    }

    private string BuildConnectionString()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _config.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = _config.CacheMode switch
            {
                CacheMode.Shared => SqliteCacheMode.Shared,
                CacheMode.Private => SqliteCacheMode.Private,
                _ => SqliteCacheMode.Default
            },
            DefaultTimeout = (int)_config.ConnectionTimeoutSeconds,
            Pooling = false // We manage our own pooling
        };

        return builder.ConnectionString;
    }

    private async Task ConfigureConnectionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        // Enable WAL mode if configured
        if (_config.UseWAL)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=WAL;";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Enable foreign keys if configured
        if (_config.EnableForeignKeys)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys=ON;";
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private void StartCleanupTask()
    {
        Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), _cts.Token);
                    CleanupIdleConnections();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error("Error during connection cleanup", ex);
                }
            }
        }, _cts.Token);
    }

    private void CleanupIdleConnections()
    {
        var validConnections = new List<PooledConnection>();

        while (_pool.TryPop(out var pooled))
        {
            if (pooled.IsValid() && !pooled.IsIdleTooLong(TimeSpan.FromMinutes(10)))
            {
                validConnections.Add(pooled);
            }
            else
            {
                pooled.Dispose();
                _logger.Trace("Disposed idle connection");
            }
        }

        foreach (var conn in validConnections)
        {
            _pool.Push(conn);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ConnectionManager));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cts.Cancel();

        while (_pool.TryPop(out var pooled))
        {
            pooled.Dispose();
        }

        _poolLock.Dispose();
        _cts.Dispose();

        _logger.Info("ConnectionManager disposed");
    }

    /// <summary>
    /// Represents a pooled database connection
    /// </summary>
    private class PooledConnection : IDisposable
    {
        public SqliteConnection Connection { get; }
        public SemaphoreSlim Lock { get; } = new(1, 1);
        public DateTime LastUsed { get; private set; }
        public bool InUse { get; private set; }

        public PooledConnection(SqliteConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            LastUsed = DateTime.UtcNow;
            InUse = false;
        }

        public void MarkInUse()
        {
            InUse = true;
            LastUsed = DateTime.UtcNow;
        }

        public void MarkAvailable()
        {
            InUse = false;
            LastUsed = DateTime.UtcNow;
        }

        public bool IsValid()
        {
            return !InUse &&
                   Connection.State == ConnectionState.Open &&
                   !IsIdleTooLong(TimeSpan.FromMinutes(10));
        }

        public bool IsIdleTooLong(TimeSpan timeout)
        {
            return DateTime.UtcNow - LastUsed > timeout;
        }

        public void Dispose()
        {
            try
            {
                Lock.Dispose();
            }
            catch { }

            try
            {
                Connection.Dispose();
            }
            catch { }
        }
    }
}
