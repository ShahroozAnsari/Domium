using System.Data;
using System.Data.Common;

namespace Domium.Persistence.Dapper;

internal sealed class DapperSession(IDapperConnectionFactory connectionFactory) :
    IDapperSession,
    IAsyncDisposable,
    IDisposable
{
    private readonly IDapperConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private DbConnection? _connection;

    public DbTransaction? CurrentTransaction { get; private set; }

    public async ValueTask<DbConnection> GetOpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_connection is null)
        {
            _connection = await _connectionFactory
                .CreateConnectionAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        return _connection;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentTransaction is not null)
        {
            return;
        }

        var connection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        CurrentTransaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentTransaction is null)
        {
            return;
        }

        await CurrentTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        await CurrentTransaction.DisposeAsync().ConfigureAwait(false);
        CurrentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentTransaction is null)
        {
            return;
        }

        await CurrentTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        await CurrentTransaction.DisposeAsync().ConfigureAwait(false);
        CurrentTransaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (CurrentTransaction is not null)
        {
            await CurrentTransaction.DisposeAsync().ConfigureAwait(false);
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        CurrentTransaction?.Dispose();
        _connection?.Dispose();
    }
}
