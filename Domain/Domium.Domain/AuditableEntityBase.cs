using Domium.Domain.Abstractions.Entity;

namespace Domium.Domain;

/// <summary>
/// Base type for entities that track creation and modification metadata.
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
public abstract class AuditableEntityBase<TId> : EntityBase<TId>, IAuditableEntity
{
    protected AuditableEntityBase(TId id)
        : base(id)
    {
    }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ModifiedAt { get; private set; }

    public string? CreatedBy { get; private set; }

    public string? ModifiedBy { get; private set; }

    public void MarkCreated(DateTimeOffset createdAt, string? createdBy = null)
    {
        CreatedAt = createdAt;
        CreatedBy = NormalizeActor(createdBy);
    }

    public void MarkModified(DateTimeOffset modifiedAt, string? modifiedBy = null)
    {
        ModifiedAt = modifiedAt;
        ModifiedBy = NormalizeActor(modifiedBy);
    }

    private static string? NormalizeActor(string? actor)
    {
        return string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
    }
}
