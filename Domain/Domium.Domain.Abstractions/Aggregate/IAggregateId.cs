using Domium.Domain.Abstractions.ValueObject;

namespace Domium.Domain.Abstractions.Aggregate;

/// <summary>
/// Marker interface for aggregate identifiers.
/// All aggregate IDs must be value objects.
/// </summary>
public interface IAggregateId : IValueObject
{
}

public interface IAggregateId<out T> : IAggregateId
{
     T Value { get; }
}