using Domium.Domain.Abstractions.Aggregate;
using Domium.Domain.Abstractions.Events;

namespace Domium.Domain;

public abstract class AggregateRoot<TId> : EntityBase<TId>, IAggregateRoot<TId>
    where TId : IAggregateId
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {
    }

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
