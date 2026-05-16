// Domium.Persistence.Abstractions/IRepository.cs

using System.Threading;
using System.Threading.Tasks;
using Domium.Domain.Abstractions.Aggregate;

namespace Domium.Persistence.Abstractions;

/// <summary>
/// Minimal repository contract for aggregate roots.
/// </summary>
public interface IRepository<TAggregate, TId>
    : IReadOnlyRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TId>
    where TId : IAggregateId
{
    /// <summary>
    /// Adds a new aggregate to the repository.
    /// </summary>
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing aggregate.
    /// </summary>
    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an aggregate from the repository.
    /// </summary>
    Task RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

}
