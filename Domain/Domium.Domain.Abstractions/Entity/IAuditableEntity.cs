namespace Domium.Domain.Abstractions.Entity;

/// <summary>
/// Interface for entities that track creation and modification timestamps.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Gets the date and time when the entity was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the date and time when the entity was last modified.
    /// </summary>
    DateTimeOffset? ModifiedAt { get; }

    /// <summary>
    /// Gets the identifier of the user who created the entity.
    /// </summary>
    string? CreatedBy { get; }

    /// <summary>
    /// Gets the identifier of the user who last modified the entity.
    /// </summary>
    string? ModifiedBy { get; }
}