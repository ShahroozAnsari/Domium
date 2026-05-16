namespace Domium.Eventing.Abstractions.External;

/// <summary>
/// Default external publisher used when no transport provider is configured.
/// </summary>
public sealed class NullExternalEventPublisher : IExternalEventPublisher
{
    public Task PublishAsync<TExternalEvent>(
        TExternalEvent externalEvent,
        CancellationToken cancellationToken = default)
        where TExternalEvent : class, IExternalEvent
    {
        if (externalEvent == null)
        {
            throw new ArgumentNullException(nameof(externalEvent));
        }

        return Task.CompletedTask;
    }
}
