using Domium.Domain.Abstractions.Aggregate;
using Domium.Persistence.EntityFrameworkCore.Specifications;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
    protected virtual IQueryable<TAggregate> Query
    => _dbContext.Set<TAggregate>();

    public EfRepository(DomiumDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbSet = _dbContext.Set<TAggregate>();
    }

    protected Task<TAggregate?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        return Query.FirstOrDefaultAsync(
             x => x.Id.Equals(id),
             cancellationToken);
    }

    protected async Task<IReadOnlyList<TAggregate>> FindAsync(
       Expression<Func<TAggregate, bool>> expression,
       CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(expression)
            .ToListAsync(cancellationToken);
    }

    protected Task<bool> AnyAsync(
        Expression<Func<TAggregate, bool>> expression,
        CancellationToken cancellationToken = default)
    {
        return _dbSet.AnyAsync(expression, cancellationToken);
    }


    protected Task<int> CountAsync(
        Expression<Func<TAggregate, bool>> expression,
        CancellationToken cancellationToken = default)
    {
        return _dbSet.CountAsync(expression, cancellationToken);
    }

    protected Task AddAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {

        return _dbSet.AddAsync(aggregate, cancellationToken).AsTask();
    }

    protected Task RemoveAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {

        _dbSet.Remove(aggregate);
        return Task.CompletedTask;
    }
}
