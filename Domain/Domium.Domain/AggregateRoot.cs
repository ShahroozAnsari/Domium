using Domium.Domain.Abstractions.Aggregate;
using Domium.Domain.Abstractions.Events;

namespace Domium.Domain;

/// <summary>
/// Base type for aggregate roots that record domain events.
/// </summary>
/// <typeparam name="TId">The aggregate identifier type.</typeparam>
public abstract class AggregateRoot<TId>(TId id) : EntityBase<TId>(id), IAggregateRoot<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));
        _domainEvents.Add(domainEvent);
    }
}
