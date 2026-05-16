using Domium.Domain.Abstractions.ValueObject;

namespace Domium.Domain.Abstractions.Aggregate;

/// <summary>
/// Marker interface for aggregate identifiers.
/// All aggregate IDs must be value objects.
/// </summary>
public interface IAggregateId : IValueObject
{
    /// <summary>
    /// Gets the underlying primitive value of the identifier.
    /// </summary>
    object Value { get; }
}

/// <summary>
/// Strongly-typed aggregate identifier.
/// </summary>
/// <typeparam name="T">The primitive type (Guid, int, string, etc.)</typeparam>
public interface IAggregateId<out T> : IAggregateId
{
    /// <summary>
    /// Gets the strongly-typed underlying value.
    /// </summary>
    new T Value { get; }
}