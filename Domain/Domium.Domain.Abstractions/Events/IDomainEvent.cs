namespace Domium.Domain.Abstractions.Events;

/// <summary>
/// Represents a domain event that occurred in the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the date and time when the event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}