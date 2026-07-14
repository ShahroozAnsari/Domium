using Domium.Domain.Abstractions.Aggregate;
using Domium.Eventing.Abstractions;

namespace Domium.Domain;

/// <summary>
/// Base type for aggregate roots. Domain events raised through <see cref="RaiseEvent"/> are
/// dispatched in-process, in the same DI scope as the current unit of work, so event handlers
/// share the caller's DbContext and their changes commit in the same transaction.
///
/// Two dispatch paths keep that guarantee:
/// - Aggregates loaded from the database get an <see cref="IEventBus"/> injected by the EF
///   materialization interceptor and publish immediately.
/// - Freshly created aggregates (via <c>new</c>) have no bus yet; their events are buffered
///   and published by <c>DomainEventDispatchInterceptor</c> right before SaveChanges — still
///   inside the same scope and transaction.
/// </summary>
public abstract class AggregateRoot<TId> : EntityBase<TId>, IAggregateRoot<TId>
    where TId : IAggregateId
{
    protected AggregateRoot()
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
    /// Publishes the event immediately when a bus is attached; otherwise buffers it for
    /// dispatch just before SaveChanges (same scope, same transaction).
    /// </summary>
    protected async Task RaiseEvent(IDomiumEvent @event)
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));

      
            await EventBus.PublishAsync(@event);

        


    }  
}
