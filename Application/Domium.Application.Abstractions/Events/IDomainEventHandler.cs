using System.Threading;
using System.Threading.Tasks;
using Domium.Domain.Abstractions.Events;

namespace Domium.Application.Abstractions.Events;

/// <summary>
/// Handles a domain event in the application layer.
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type.</typeparam>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// Handles a domain event.
    /// </summary>
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
