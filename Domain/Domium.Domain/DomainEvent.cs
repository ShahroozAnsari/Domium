using Domium.Domain.Abstractions.Events;

namespace Domium.Domain;

/// <summary>
/// Base type for domain events.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    protected DomainEvent()
        : this(Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }

    protected DomainEvent(Guid eventId, DateTimeOffset occurredOn)
    {
        EventId = eventId;
        OccurredOn = occurredOn;
    }

    public Guid EventId { get; }

    public DateTimeOffset OccurredOn { get; }
}
