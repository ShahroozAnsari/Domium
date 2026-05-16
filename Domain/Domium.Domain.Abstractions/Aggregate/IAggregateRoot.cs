using Domium.Domain.Abstractions.Entity;
using Domium.Domain.Abstractions.Events;

namespace Domium.Domain.Abstractions.Aggregate;

/// <summary>
/// Marker interface for aggregate roots.
/// Aggregate roots are the entry points for all operations on an aggregate.
/// </summary>
public interface IAggregateRoot : IEntityBase
{
    /// <summary>
    /// Gets the collection of domain events raised by this aggregate.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all domain events from this aggregate.
    /// </summary>
    void ClearDomainEvents();
}

/// <summary>
/// Aggregate root with a strongly-typed identifier.
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
public interface IAggregateRoot<TId> : IAggregateRoot, IEntityBase<TId> 
{
}
