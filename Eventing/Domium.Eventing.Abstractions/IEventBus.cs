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

    /// <summary>
    /// Registers an ad-hoc listener for the current scope. Dispose the returned token to remove it.
    /// Listeners run after the DI-registered handlers for the same event.
    /// </summary>
    IDisposable Subscribe<TEvent>(
        Func<TEvent, CancellationToken, Task> listener)
        where TEvent : IDomiumEvent;
}
