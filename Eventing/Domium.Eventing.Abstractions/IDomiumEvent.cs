namespace Domium.Eventing.Abstractions;

/// <summary>
/// Marker interface for all Domium events.
/// </summary>
public interface IDomiumEvent
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
