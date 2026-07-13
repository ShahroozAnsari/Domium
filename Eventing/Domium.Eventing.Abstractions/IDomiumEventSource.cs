using System.Collections.Generic;

namespace Domium.Eventing.Abstractions;

/// <summary>
/// Implemented by aggregates that can buffer domain events when no <see cref="IEventBus"/>
/// has been attached yet (typically freshly created aggregates that have not passed through
/// EF materialization). Buffered events are drained and published by the persistence layer
/// right before SaveChanges, on the same DbContext scope and transaction.
/// </summary>
public interface IDomiumEventSource
{
    /// <summary>The events raised so far that have not been dispatched yet.</summary>
    IReadOnlyCollection<IDomiumEvent> PendingDomainEvents { get; }

    /// <summary>Removes and returns all pending events, leaving the buffer empty.</summary>
    IReadOnlyList<IDomiumEvent> DequeuePendingDomainEvents();
}
