using Domium.Domain.Abstractions.Entity;

namespace Domium.Domain;

/// <summary>
/// Base type for domain entities.
/// </summary>
public abstract class EntityBase : IEntityBase
{
}

/// <summary>
/// Base type for entities with a strongly typed identifier.
/// </summary>
/// <typeparam name="TId">The identifier type.</typeparam>
public abstract class EntityBase<TId> : EntityBase, IEntityBase<TId>
{
    protected EntityBase(TId id)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        Id = id;
    }

    public TId Id { get; protected set; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not EntityBase<TId> other)
        {
            return false;
        }

        return GetType() == other.GetType() &&
               EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() =>
        HashCode.Combine(GetType(), Id);

    public static bool operator ==(EntityBase<TId>? left, EntityBase<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(EntityBase<TId>? left, EntityBase<TId>? right)
    {
        return !Equals(left, right);
    }
}
