using Domium.Eventing.Abstractions.Internal;

namespace Domium.Domain.Abstractions.Events;

/// <summary>
/// Represents a domain event that occurred inside the current domain model.
/// </summary>
public interface IDomainEvent : IInternalEvent
{
}
