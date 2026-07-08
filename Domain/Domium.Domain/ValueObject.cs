using Domium.Domain.Abstractions.ValueObject;

namespace Domium.Domain;

/// <summary>
/// Base type for immutable value objects compared by their components.
/// </summary>
public abstract class ValueObject : IValueObject
{
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(GetType());

        foreach (var component in GetEqualityComponents())
        {
            hashCode.Add(component);
        }

        return hashCode.ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    /// Gets the components that define this value object's equality.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();
}
