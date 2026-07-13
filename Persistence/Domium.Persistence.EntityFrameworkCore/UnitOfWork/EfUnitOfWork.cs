using Domium.Domain;
using Domium.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation of the Domium unit of work.
/// Begin/Commit pairs may nest (e.g. a command handler dispatching another command); only
/// the outermost pair opens, saves, and commits the database transaction.
/// <see cref="ExecuteAsync"/> additionally runs the whole unit through the context's
/// execution strategy, making it compatible with EnableRetryOnFailure.
/// </summary>
public sealed class EfUnitOfWork(DomiumDbContext dbContext) : IUnitOfWork, IAsyncDisposable
{
    private readonly DomiumDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private IDbContextTransaction? _transaction;
    private int _depth;

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            _depth++;
            return;
        }

        _transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        _depth = 1;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null && _depth > 1)
        {
            // Inner scope: defer saving and committing to the outermost owner.
            _depth--;
            return;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new DomiumConcurrencyException(
                "The aggregate was modified by another operation since it was loaded.",
                exception);
        }

        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            await _transaction.DisposeAsync().ConfigureAwait(false);
            _transaction = null;
            _depth = 0;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        // A rollback aborts the whole transaction regardless of nesting depth.
        await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        await _transaction.DisposeAsync().ConfigureAwait(false);
        _transaction = null;
        _depth = 0;
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        if (_transaction is not null)
        {
            // Already inside a unit of work (nested command) — join the ongoing transaction;
            // the outermost owner saves and commits.
            await RunUnitAsync(operation, cancellationToken).ConfigureAwait(false);
            return;
        }

        // The execution strategy re-runs the whole unit on transient failures when a
        // retrying strategy (EnableRetryOnFailure) is configured; the default strategy
        // executes it exactly once.
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(
            () => RunUnitAsync(operation, cancellationToken)).ConfigureAwait(false);
    }

    private async Task RunUnitAsync(Func<Task> operation, CancellationToken cancellationToken)
    {
        await BeginAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await operation().ConfigureAwait(false);
            await CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // The incoming token may already be cancelled; the compensating rollback must
            // still run, so it gets a token that cannot be cancelled.
            await RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync().ConfigureAwait(false);
        }
    }
}
