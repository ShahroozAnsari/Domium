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
    private readonly List<IDomiumEvent> _pendingDomainEvents = new();

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
    /// Events raised while no bus was attached, awaiting dispatch just before SaveChanges.
    /// </summary>
    public IReadOnlyList<IDomiumEvent> PendingDomainEvents => _pendingDomainEvents;

    /// <summary>
    /// Returns the buffered events and clears the buffer. Called by the dispatch
    /// interceptor right before SaveChanges, inside the same scope and transaction.
    /// </summary>
    public IReadOnlyList<IDomiumEvent> DequeuePendingDomainEvents()
    {
        var events = _pendingDomainEvents.ToArray();
        _pendingDomainEvents.Clear();
        return events;
    }

    /// <summary>
    /// Publishes the event immediately when a bus is attached; otherwise buffers it for
    /// dispatch just before SaveChanges (same scope, same transaction).
    /// </summary>
    protected async Task RaiseEvent(IDomiumEvent @event)
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));

        if (EventBus != null)
        {
            await EventBus.PublishAsync(@event);
        }
        else
        {
            _pendingDomainEvents.Add(@event);
        }
    }
}
