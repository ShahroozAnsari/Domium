using Domium.Domain.Abstractions.Events;

namespace Domium.Domain;

/// <summary>
/// Base type for domain events.
/// </summary>
public abstract class DomainEvent(Guid eventId, DateTimeOffset occurredOn) : IDomainEvent
{
    protected DomainEvent()
        : this(Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }

    public Guid EventId { get; } = eventId;

    public DateTimeOffset OccurredOn { get; } = occurredOn;
}
