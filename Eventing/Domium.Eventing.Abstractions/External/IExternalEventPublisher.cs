namespace Domium.Eventing.Abstractions.External;

/// <summary>
/// Publishes external events through the configured transport provider.
/// </summary>
public interface IExternalEventPublisher
{
    /// <summary>
    /// Publishes an external event.
    /// </summary>
    Task PublishAsync<TExternalEvent>(
        TExternalEvent externalEvent,
        CancellationToken cancellationToken = default)
        where TExternalEvent : class, IExternalEvent;
}
