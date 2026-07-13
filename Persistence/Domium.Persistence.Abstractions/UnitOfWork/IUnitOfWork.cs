using System;
using System.Threading;
using System.Threading.Tasks;

namespace Domium.Persistence.Abstractions;

public interface IUnitOfWork
{
    Task BeginAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a Begin/Commit pair, rolling back on failure.
    /// Providers with retrying execution strategies (e.g. EF Core with EnableRetryOnFailure)
    /// execute the whole unit through the strategy, so the operation must be safe to re-run
    /// when a transient failure retries the transaction.
    /// </summary>
    Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
