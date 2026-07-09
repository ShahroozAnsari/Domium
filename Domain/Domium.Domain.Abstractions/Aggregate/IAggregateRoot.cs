using Domium.Domain.Abstractions.Entity;
using Domium.Domain.Abstractions.Events;

namespace Domium.Domain.Abstractions.Aggregate;

public interface IAggregateRoot : IEntityBase
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

/// <summary>
/// Aggregate root with a strongly-typed identifier.
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
public interface IAggregateRoot<TId> : IAggregateRoot, IEntityBase<TId>
    where TId : IAggregateId
{
}
