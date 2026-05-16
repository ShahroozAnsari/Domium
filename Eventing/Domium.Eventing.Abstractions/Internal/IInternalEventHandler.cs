namespace Domium.Eventing.Abstractions.Internal;

/// <summary>
/// Handles an internal event.
/// </summary>
/// <typeparam name="TInternalEvent">The internal event type.</typeparam>
public interface IInternalEventHandler<in TInternalEvent>
    where TInternalEvent : IInternalEvent
{
    /// <summary>
    /// Handles the internal event.
    /// </summary>
    Task HandleAsync(TInternalEvent internalEvent, CancellationToken cancellationToken = default);
}
