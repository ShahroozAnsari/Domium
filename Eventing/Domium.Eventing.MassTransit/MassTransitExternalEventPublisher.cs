using Domium.Eventing.Abstractions.External;
using MassTransit;

namespace Domium.Eventing.MassTransit;

/// <summary>
/// Publishes Domium external events through MassTransit.
/// </summary>
public sealed class MassTransitExternalEventPublisher(IPublishEndpoint publishEndpoint) : IExternalEventPublisher
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

        return publishEndpoint.Publish(externalEvent, cancellationToken);
    }
}
