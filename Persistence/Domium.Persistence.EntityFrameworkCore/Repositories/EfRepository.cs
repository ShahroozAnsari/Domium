using System.Linq.Expressions;
using Domium.Domain.Abstractions.Aggregate;
using Domium.Persistence.Abstractions;
using Domium.Persistence.Abstractions.Specifications;
using Domium.Persistence.EntityFrameworkCore.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// EF Core aggregate repository. Writes go through the change tracker and are persisted by
/// the ambient unit of work / SaveChanges; reads support both expressions and specifications.
/// </summary>
public class EfRepository<TAggregate, TId> : ISpecificationRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TId>
    where TId : IAggregateId
{
    private readonly DomiumDbContext _dbContext;

    public EfRepository(DomiumDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    protected virtual IQueryable<TAggregate> Query => _dbContext.Set<TAggregate>();

    public virtual async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));

        return await Query
            .FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task<IReadOnlyList<TAggregate>> FindAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        return await EfSpecificationEvaluator.GetQuery(Query, specification)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual Task<int> CountAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        return EfSpecificationEvaluator.GetQuery(Query, specification).CountAsync(cancellationToken);
    }

    public virtual Task<bool> AnyAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        return EfSpecificationEvaluator.GetQuery(Query, specification).AnyAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TAggregate>> FindAsync(
        Expression<Func<TAggregate, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(predicate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public virtual async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

        await _dbContext.Set<TAggregate>().AddAsync(aggregate, cancellationToken).ConfigureAwait(false);
    }

    public virtual Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

        // Tracked aggregates persist automatically on SaveChanges; Update covers detached ones.
        if (_dbContext.Entry(aggregate).State == EntityState.Detached)
        {
            _dbContext.Set<TAggregate>().Update(aggregate);
        }

        return Task.CompletedTask;
    }

    public virtual Task RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

        _dbContext.Set<TAggregate>().Remove(aggregate);
        return Task.CompletedTask;
    }
}
