// IReadOnlyRepository.cs

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domium.Domain.Abstractions.Aggregate;

namespace Domium.Persistence.Abstractions;

/// <summary>
/// Represents a read-only repository for querying aggregate roots.
/// </summary>
public interface IReadOnlyRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TId>
    where TId : IAggregateId
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

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