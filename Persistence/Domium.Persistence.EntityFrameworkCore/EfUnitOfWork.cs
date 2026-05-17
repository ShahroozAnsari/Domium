using Domium.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation of the Domium unit of work.
/// </summary>
public sealed class EfUnitOfWork(DbContext dbContext) : IUnitOfWork, IAsyncDisposable
{
    private readonly DbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private IDbContextTransaction? _transaction;

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            return;
        }

        _transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_dbContext is DomiumDbContext domiumDbContext)
        {
            await CommitDomiumDbContextAsync(domiumDbContext, cancellationToken).ConfigureAwait(false);
            return;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            await _transaction.DisposeAsync().ConfigureAwait(false);
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        await _transaction.DisposeAsync().ConfigureAwait(false);
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task CommitDomiumDbContextAsync(
        DomiumDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var domainEvents = dbContext.CaptureDomainEvents();

        using (dbContext.SuppressDomainEventDispatch())
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            await _transaction.DisposeAsync().ConfigureAwait(false);
            _transaction = null;
        }

        await dbContext.DispatchDomainEventsAsync(domainEvents, cancellationToken).ConfigureAwait(false);
        DomiumDbContext.ClearDomainEvents(domainEvents);
    }
}
