namespace Domium.Eventing.Abstractions.External;

/// <summary>
/// Handles an external event after it is received from a transport.
/// </summary>
/// <typeparam name="TExternalEvent">The external event type.</typeparam>
public interface IExternalEventHandler<in TExternalEvent>
    where TExternalEvent : class, IExternalEvent
{
    /// <summary>
    /// Handles the external event.
    /// </summary>
    Task HandleAsync(TExternalEvent externalEvent, CancellationToken cancellationToken = default);
}
