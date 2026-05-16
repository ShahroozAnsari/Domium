using Domium.Domain;
using Domium.Domain.Abstractions.Events;

namespace Domium.Tests.Domain;

public sealed class DomainPrimitiveTests
{
    [Fact]
    public void Entity_equality_uses_concrete_type_and_identifier()
    {
        var first = new Customer(new CustomerId(Guid.Parse("11111111-1111-1111-1111-111111111111")));
        var second = new Customer(new CustomerId(Guid.Parse("11111111-1111-1111-1111-111111111111")));
        var otherType = new Order(new CustomerId(Guid.Parse("11111111-1111-1111-1111-111111111111")));

        Assert.Equal(first, second);
        Assert.NotEqual<EntityBase<CustomerId>>(first, otherType);
    }

    [Fact]
    public void Value_object_equality_uses_components()
    {
        var first = new Money("USD", 12.5m);
        var second = new Money("USD", 12.5m);
        var different = new Money("EUR", 12.5m);

        Assert.Equal(first, second);
        Assert.NotEqual(first, different);
    }

    [Fact]
    public void Aggregate_root_records_and_clears_domain_events()
    {
        var customer = new Customer(new CustomerId(Guid.NewGuid()));

        customer.Activate();

        var domainEvent = Assert.Single(customer.DomainEvents);
        Assert.IsType<CustomerActivatedDomainEvent>(domainEvent);

        customer.ClearDomainEvents();

        Assert.Empty(customer.DomainEvents);
    }

    private sealed class CustomerId(Guid value) : AggregateId<Guid>(value);

    private sealed class Customer(CustomerId id) : AggregateRoot<CustomerId>(id)
    {
        public void Activate()
        {
            RaiseDomainEvent(new CustomerActivatedDomainEvent(Id));
        }
    }

    private sealed class Order(CustomerId id) : EntityBase<CustomerId>(id);

    private sealed class Money(string currency, decimal amount) : ValueObject
    {
        public string Currency { get; } = currency;

        public decimal Amount { get; } = amount;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Currency;
            yield return Amount;
        }
    }

    private sealed class CustomerActivatedDomainEvent(CustomerId customerId) : DomainEvent
    {
        public CustomerId CustomerId { get; } = customerId;
    }
}
