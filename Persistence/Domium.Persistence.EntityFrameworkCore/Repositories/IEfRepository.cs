using Domium.Domain.Abstractions.Aggregate;
using Domium.Persistence.Abstractions;
using Domium.Persistence.EntityFrameworkCore.Specifications;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// EF-specific aggregate repository with queryable specification support.
/// </summary>
public interface IEfRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TId>
    where TId : IAggregateId
{
    Task<IReadOnlyList<TAggregate>> FindAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);
}
