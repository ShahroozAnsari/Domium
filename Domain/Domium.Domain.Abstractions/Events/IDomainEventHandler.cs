using System.Threading;
using System.Threading.Tasks;

namespace Domium.Domain.Abstractions.Events;

/// <summary>
/// Handles a specific domain event type.
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
