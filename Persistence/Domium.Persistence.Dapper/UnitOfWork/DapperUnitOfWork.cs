using Domium.Persistence.Abstractions;

namespace Domium.Persistence.Dapper;

/// <summary>
/// Dapper implementation of Domium's unit of work abstraction. Begin/Commit pairs may nest
/// (matching the EF Core implementation); only the outermost pair touches the session's
/// transaction.
/// </summary>
internal sealed class DapperUnitOfWork(DapperSession session) : IUnitOfWork
{
    private readonly DapperSession _session =
        session ?? throw new ArgumentNullException(nameof(session));

    private int _depth;

    public Task BeginAsync(CancellationToken cancellationToken = default)
    {
        return _depth++ == 0
            ? _session.BeginTransactionAsync(cancellationToken)
            : Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_depth == 0)
        {
            return _session.CommitTransactionAsync(cancellationToken);
        }

        return --_depth == 0
            ? _session.CommitTransactionAsync(cancellationToken)
            : Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        // A rollback aborts the whole transaction regardless of nesting depth.
        _depth = 0;
        return _session.RollbackTransactionAsync(cancellationToken);
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        await BeginAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await operation().ConfigureAwait(false);
            await CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }
}
