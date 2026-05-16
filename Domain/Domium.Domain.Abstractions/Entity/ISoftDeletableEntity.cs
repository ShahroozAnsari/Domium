namespace Domium.Domain.Abstractions.Entity;

/// <summary>
/// Interface for entities that support soft deletion.
/// Soft-deleted entities are marked as deleted but remain in the database.
/// </summary>
public interface ISoftDeletableEntity
{
    /// <summary>
    /// Gets a value indicating whether the entity is deleted.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Gets the UTC date and time when the entity was deleted.
    /// Null if the entity is not deleted.
    /// </summary>
    DateTimeOffset? DeletedAt { get; }

    /// <summary>
    /// Gets the identifier of the user who deleted the entity.
    /// Null if the entity is not deleted or deleter is unknown.
    /// </summary>
    string? DeletedBy { get; }
}