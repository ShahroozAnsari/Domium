namespace Domium.Eventing.Abstractions;

/// <summary>
/// Publishes in-process Domium events to their registered handlers.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes a single event.
    /// </summary>
    Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : IDomiumEvent;

    /// <summary>
    /// Publishes a batch of events in order.
    /// </summary>
    Task PublishAsync(
        IReadOnlyCollection<IDomiumEvent> events,
        CancellationToken cancellationToken = default);
}
