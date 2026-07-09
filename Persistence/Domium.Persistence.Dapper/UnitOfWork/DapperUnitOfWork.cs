using Domium.Persistence.Abstractions;

namespace Domium.Persistence.Dapper;

/// <summary>
/// Dapper implementation of Domium's unit of work abstraction.
/// </summary>
internal sealed class DapperUnitOfWork(DapperSession session) : IUnitOfWork
{
    private readonly DapperSession _session =
        session ?? throw new ArgumentNullException(nameof(session));

    public Task BeginAsync(CancellationToken cancellationToken = default)
    {
        return _session.BeginTransactionAsync(cancellationToken);
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return _session.CommitTransactionAsync(cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        return _session.RollbackTransactionAsync(cancellationToken);
    }
}
