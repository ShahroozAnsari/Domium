using Domium.Domain.Abstractions.Aggregate;

namespace Domium.Persistence.Abstractions;

/// <summary>
/// Marker contract for persistence configuration owned by an aggregate root.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type being configured.</typeparam>
public interface IEntityConfiguration<TAggregate>
    where TAggregate : class, IAggregateRoot
{
}
