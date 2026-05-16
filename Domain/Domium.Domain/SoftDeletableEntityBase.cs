using Domium.Domain.Abstractions.Entity;

namespace Domium.Domain;

/// <summary>
/// Base type for entities that support soft deletion.
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
public abstract class SoftDeletableEntityBase<TId> : AuditableEntityBase<TId>, ISoftDeletableEntity
{
    protected SoftDeletableEntityBase(TId id)
        : base(id)
    {
    }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    public void MarkDeleted(DateTimeOffset deletedAtUtc, string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
        DeletedBy = string.IsNullOrWhiteSpace(deletedBy) ? null : deletedBy.Trim();
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
    }
}
