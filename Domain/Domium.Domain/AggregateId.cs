using Domium.Domain.Abstractions.Aggregate;

namespace Domium.Domain;

public abstract class AggregateId<T> : ValueObject, IAggregateId<T>
{
    protected AggregateId(T value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (value is Guid guid && guid == Guid.Empty)
        {
            throw new ArgumentException($"{GetType().Name} cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public T Value { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value?.ToString() ?? string.Empty;
    }

}
