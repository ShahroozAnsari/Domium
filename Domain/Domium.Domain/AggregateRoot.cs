using Domium.Domain.Abstractions.Aggregate;
using Domium.Eventing.Abstractions;

namespace Domium.Domain;

public abstract class AggregateRoot<TId> : EntityBase<TId>, IAggregateRoot<TId>
    where TId : IAggregateId
{
    private IEventBus? _eventBus;

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot(TId id, IEventBus eventBus) : base(id)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    protected IEventBus EventBus =>
        _eventBus
        ?? throw new InvalidOperationException(
            $"Aggregate '{GetType().Name}' cannot publish events because no event bus was provided.");

    public void AttachEventBus(IEventBus eventBus)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    protected void RaiseEvent(IDomiumEvent @event)
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));

        EventBus.PublishAsync(@event).GetAwaiter().GetResult();
    }
}
