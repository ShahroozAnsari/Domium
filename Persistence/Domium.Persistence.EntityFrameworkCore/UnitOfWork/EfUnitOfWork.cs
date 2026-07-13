using Domium.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation of the Domium unit of work.
/// Begin/Commit pairs may nest (e.g. a command handler dispatching another command); only
/// the outermost pair opens, saves, and commits the database transaction.
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

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync().ConfigureAwait(false);
        }
    }
}
