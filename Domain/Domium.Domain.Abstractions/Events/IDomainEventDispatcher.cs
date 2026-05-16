using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Domium.Domain.Abstractions.Events;

/// <summary>
/// Dispatches domain events to their registered handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches the supplied domain events.
    /// </summary>
    Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}
