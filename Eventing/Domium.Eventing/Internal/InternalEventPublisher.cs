using Domium.Eventing.Abstractions.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Eventing.Internal;

/// <summary>
/// Publishes internal events to handlers registered in the current service provider.
/// </summary>
public sealed class InternalEventPublisher(IServiceProvider serviceProvider) : IInternalEventPublisher
{
    public async Task PublishAsync<TInternalEvent>(
        TInternalEvent internalEvent,
        CancellationToken cancellationToken = default)
        where TInternalEvent : IInternalEvent
    {
        if (internalEvent == null)
        {
            throw new ArgumentNullException(nameof(internalEvent));
        }

        var handlerType = typeof(IInternalEventHandler<>).MakeGenericType(internalEvent.GetType());
        var handlers = serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var handleMethod = handlerType.GetMethod(nameof(IInternalEventHandler<IInternalEvent>.HandleAsync));

            if (handleMethod is null)
            {
                continue;
            }

            var task = (Task?)handleMethod.Invoke(handler, new object[] { internalEvent, cancellationToken });

            if (task is not null)
            {
                await task.ConfigureAwait(false);
            }
        }
    }
}
