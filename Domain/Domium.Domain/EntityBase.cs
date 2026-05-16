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
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    public TId Id { get; protected init; }

    public override bool Equals(object? obj)
    {
        if (obj is not EntityBase<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetType() == other.GetType() && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(EntityBase<TId>? left, EntityBase<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(EntityBase<TId>? left, EntityBase<TId>? right)
    {
        return !Equals(left, right);
    }
}
