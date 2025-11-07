
using CodeLogic.Abstractions;
using MySqlConnector;

namespace CL.MySQL2.Services;

/// <summary>
/// Manages a database transaction, ensuring it is either committed or rolled back.
/// This class is designed to be used within a 'using' statement.
/// </summary>
public class TransactionScope : IAsyncDisposable
{
    internal string ConnectionId { get; }
    internal MySqlConnection Connection { get; }
    internal MySqlTransaction Transaction { get; }
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger? _logger;
    private bool _isCompleted = false;

    internal TransactionScope(
        MySqlConnection connection, 
        MySqlTransaction transaction, 
        ConnectionManager connectionManager, 
        string connectionId,
        ILogger? logger)
    {
        Connection = connection;
        Transaction = transaction;
        _connectionManager = connectionManager;
        ConnectionId = connectionId;
        _logger = logger;
    }

    /// <summary>
    /// Commits the transaction to the database.
    /// </summary>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_isCompleted)
        {
            throw new InvalidOperationException("This transaction has already been completed.");
        }

        await Transaction.CommitAsync(cancellationToken);
        _isCompleted = true;
        _logger?.Debug($"Transaction {Transaction.GetHashCode()} committed for connection '{ConnectionId}'.");
    }

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_isCompleted)
        {
            throw new InvalidOperationException("This transaction has already been completed.");
        }

        await Transaction.RollbackAsync(cancellationToken);
        _isCompleted = true;
        _logger?.Warning($"Transaction {Transaction.GetHashCode()} rolled back for connection '{ConnectionId}'.");
    }

    /// <summary>
    /// Disposes the transaction scope. If the transaction was not explicitly committed or rolled back,
    /// it will be automatically rolled back. The underlying database connection is then closed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_isCompleted)
        {
            _logger?.Warning($"Transaction {Transaction.GetHashCode()} was not explicitly completed and will be rolled back.");
            await RollbackAsync();
        }

        await _connectionManager.CloseConnectionAsync(Connection);
    }
}
