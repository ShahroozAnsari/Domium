using Domium.Domain.Abstractions.Aggregate;
using Domium.Persistence.EntityFrameworkCore.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// EF Core repository implementation for aggregate roots.
/// </summary>
public class EfRepository<TAggregate, TId> 
    where TAggregate : class, IAggregateRoot<TId>
    where TId : IAggregateId
{
    private readonly DomiumDbContext _dbContext;
    protected readonly DbSet<TAggregate> _dbSet;

    public EfRepository(DomiumDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbSet = _dbContext.Set<TAggregate>();
    }

    protected async Task<TAggregate?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        return await _dbSet.FindAsync(new object?[] { id }, cancellationToken).ConfigureAwait(false);
    }

    protected async Task<IReadOnlyList<TAggregate>> FindAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        var query = EfSpecificationEvaluator.GetQuery(_dbSet.AsQueryable(), specification);
        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    protected async Task<bool> AnyAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        var query = EfSpecificationEvaluator.GetQuery(_dbSet.AsQueryable(), specification);
        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    protected async Task<int> CountAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        var query = EfSpecificationEvaluator.GetQuery(_dbSet.AsQueryable(), specification);
        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    protected async Task AddAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        if (aggregate == null)
        {
            throw new ArgumentNullException(nameof(aggregate));
        }

        await _dbSet.AddAsync(aggregate, cancellationToken).ConfigureAwait(false);
    }

    protected Task RemoveAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        if (aggregate == null)
        {
            throw new ArgumentNullException(nameof(aggregate));
        }

        _dbSet.Remove(aggregate);
        return Task.CompletedTask;
    }
}
