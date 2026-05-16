namespace Domium.Eventing.Abstractions.Internal;

/// <summary>
/// Publishes internal events to in-process handlers.
/// </summary>
public interface IInternalEventPublisher
{
    /// <summary>
    /// Publishes an internal event.
    /// </summary>
    Task PublishAsync<TInternalEvent>(
        TInternalEvent internalEvent,
        CancellationToken cancellationToken = default)
        where TInternalEvent : IInternalEvent;
}
