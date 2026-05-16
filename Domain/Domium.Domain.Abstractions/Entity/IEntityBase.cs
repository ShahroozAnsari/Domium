namespace Domium.Domain.Abstractions.Entity;

/// <summary>
/// Base interface for all entities in the domain.
/// </summary>
public interface IEntityBase
{
}

/// <summary>
/// Base interface for entities with a strongly-typed identifier.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public interface IEntityBase<TId> : IEntityBase
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    TId Id { get; }
}