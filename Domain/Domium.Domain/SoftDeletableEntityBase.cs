using Domium.Domain.Abstractions.Entity;

namespace Domium.Domain;

/// <summary>
/// Base type for entities that support soft deletion.
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
public abstract class SoftDeletableEntityBase<TId>(TId id) : AuditableEntityBase<TId>(id), ISoftDeletableEntity
{
    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public string? DeletedBy { get; private set; }

    public void MarkDeleted(DateTimeOffset deletedAtUtc, string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = deletedAtUtc;
        DeletedBy = string.IsNullOrWhiteSpace(deletedBy) ? null : deletedBy.Trim();
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}
