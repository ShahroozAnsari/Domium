using Domium.Domain.Abstractions.Aggregate;
using Domium.Eventing.Abstractions;

namespace Domium.Domain;

public abstract class AggregateRoot<TId> : EntityBase<TId>, IAggregateRoot<TId>
    where TId : IAggregateId
{
    internal AggregateRoot()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot(TId id, IEventBus eventBus) : base(id)
    {
        EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    protected IEventBus EventBus { get; private set; } = default!;

    protected void RaiseEvent(IDomiumEvent @event)
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));
        if (EventBus == null)
        {
            throw new InvalidOperationException(
                $"Event bus was not provided for aggregate '{GetType().Name}'.");
        }

        EventBus.PublishAsync(@event).GetAwaiter().GetResult();
    }
}
