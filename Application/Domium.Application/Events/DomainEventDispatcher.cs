using Domium.Domain.Abstractions.Events;
using Domium.Eventing.Abstractions.Internal;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Domium.Application.Events;

/// <summary>
/// Dispatches domain events through handlers registered in the service provider.
/// </summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        if (domainEvents == null) throw new ArgumentNullException(nameof(domainEvents));

        var internalEventPublisher = serviceProvider.GetService<IInternalEventPublisher>();

        foreach (var domainEvent in domainEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (internalEventPublisher is not null)
            {
                await internalEventPublisher.PublishAsync(domainEvent, cancellationToken).ConfigureAwait(false);
            }

            await DispatchSingleAsync(domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchSingleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));

        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));

            if (handleMethod is null)
            {
                continue;
            }

            Task? task;

            try
            {
                task = (Task?)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken });
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                throw exception.InnerException;
            }

            if (task is not null)
            {
                await task.ConfigureAwait(false);
            }
        }
    }
}
