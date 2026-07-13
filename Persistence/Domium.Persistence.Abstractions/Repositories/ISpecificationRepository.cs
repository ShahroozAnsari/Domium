using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domium.Domain.Abstractions.Aggregate;
using Domium.Persistence.Abstractions.Specifications;

namespace Domium.Persistence.Abstractions;

/// <summary>
/// Specification-based reads for LINQ-capable providers (EF Core).
/// </summary>
public interface ISpecificationRepository<TAggregate, in TId> : IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TId>
    where TId : IAggregateId
{
    Task<IReadOnlyList<TAggregate>> FindAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);

    Task<int> CountAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);
}
