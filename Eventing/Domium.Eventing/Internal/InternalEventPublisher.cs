using Domium.Eventing.Abstractions;
using Domium.Eventing.Abstractions.Internal;

namespace Domium.Eventing.Internal;

/// <summary>
/// Publishes internal events to handlers registered in the current service provider.
/// </summary>
public sealed class InternalEventPublisher(IEventBus eventBus) : IInternalEventPublisher
{
    public Task PublishAsync<TInternalEvent>(
        TInternalEvent internalEvent,
        CancellationToken cancellationToken = default)
        where TInternalEvent : IInternalEvent
    {
        if (internalEvent == null)
        {
            throw new ArgumentNullException(nameof(internalEvent));
        }

        return eventBus.PublishAsync(internalEvent, cancellationToken);
    }
}
