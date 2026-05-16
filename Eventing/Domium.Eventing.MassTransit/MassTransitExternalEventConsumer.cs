using Domium.Eventing.Abstractions.External;
using MassTransit;

namespace Domium.Eventing.MassTransit;

/// <summary>
/// MassTransit consumer that forwards external events to Domium external event handlers.
/// </summary>
/// <typeparam name="TExternalEvent">The external event type.</typeparam>
public sealed class MassTransitExternalEventConsumer<TExternalEvent>(
    IEnumerable<IExternalEventHandler<TExternalEvent>> handlers)
    : IConsumer<TExternalEvent>
    where TExternalEvent : class, IExternalEvent
{
    public async Task Consume(ConsumeContext<TExternalEvent> context)
    {
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(context.Message, context.CancellationToken).ConfigureAwait(false);
        }
    }
}
