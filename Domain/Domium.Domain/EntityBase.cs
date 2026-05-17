using Domium.Domain.Abstractions.Entity;
using Domium.Domain.Abstractions.Aggregate;
using System.Runtime.CompilerServices;

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
        if (obj is not EntityBase<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (HasDefaultIdentity(Id) || HasDefaultIdentity(other.Id))
        {
            return false;
        }

        return GetType() == other.GetType() && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        if (HasDefaultIdentity(Id))
        {
            return RuntimeHelpers.GetHashCode(this);
        }

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

    private static bool HasDefaultIdentity(TId id)
    {
        if (id is not IAggregateId aggregateId)
        {
            return EqualityComparer<TId>.Default.Equals(id, default!);
        }

        var value = aggregateId.Value;

        if (value is null)
        {
            return true;
        }

        if (value is string text)
        {
            return string.IsNullOrWhiteSpace(text);
        }

        var valueType = value.GetType();

        return valueType.IsValueType &&
               value.Equals(Activator.CreateInstance(valueType));
    }
}
