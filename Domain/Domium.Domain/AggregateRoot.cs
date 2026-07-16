using Domium.Domain.Abstractions.Aggregate;
using Domium.Eventing.Abstractions;

namespace Domium.Domain;

/// <summary>
/// Base type for aggregate roots. Domain events raised through <see cref="RaiseEvent"/> are
/// always published straight to the <see cref="IEventBus"/> — in-process, in the same DI scope
/// as the current unit of work, so event handlers share the caller's DbContext and their
/// changes commit in the same transaction. There is no buffering.
///
/// Two ways an aggregate gets its bus:
/// - Aggregates loaded from the database get an <see cref="IEventBus"/> injected by the EF
///   materialization interceptor.
/// - Freshly created aggregates receive the bus through their static creational factory,
///   which passes it to the <c>(id, eventBus)</c> constructor and raises the creation event
///   via <see cref="CreateAsync{TAggregate}"/>.
/// </summary>
public abstract class AggregateRoot<TId> : EntityBase<TId>, IAggregateRoot<TId>
    where TId : IAggregateId
{

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot(TId id, IEventBus eventBus) : base(id)
    {
        EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    /// <summary>
    /// The event bus for immediate in-scope dispatch. Injected by the EF materialization
    /// interceptor for loaded aggregates, or via the constructor.
    /// </summary>
    protected IEventBus? EventBus { get; private set; }

    /// <summary>
    /// Creational helper for derived aggregates whose construction must raise an event:
    /// build the aggregate with the <c>(id, eventBus)</c> constructor, then let this helper
    /// publish the creation event and hand the aggregate back.
    /// </summary>
    protected static async Task<TAggregate> CreateAsync<TAggregate>(
        TAggregate aggregate,
        IDomiumEvent creationEvent)
        where TAggregate : AggregateRoot<TId>
    {
        if (aggregate == null) throw new ArgumentNullException(nameof(aggregate));

        await aggregate.RaiseEvent(creationEvent);
        return aggregate;
    }

    /// <summary>
    /// Publishes the event immediately on the attached bus. Throws when no bus is attached —
    /// load the aggregate through persistence or create it via its static factory.
    /// </summary>
    protected async Task RaiseEvent(IDomiumEvent @event)
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));

        if (EventBus == null)
        {
            throw new InvalidOperationException(
                "No event bus is attached to this aggregate. Load it through persistence " +
                "(the materialization interceptor attaches the bus) or create it via its " +
                "static creational factory.");
        }

        await EventBus.PublishAsync(@event);
    }
}
