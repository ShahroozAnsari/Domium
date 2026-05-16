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

    private sealed class CustomerId : AggregateId<Guid>
    {
        public CustomerId(Guid value)
            : base(value)
        {
        }
    }

    private sealed class Customer : AggregateRoot<CustomerId>
    {
        public Customer(CustomerId id)
            : base(id)
        {
        }

        public void Activate()
        {
            RaiseDomainEvent(new CustomerActivatedDomainEvent(Id));
        }
    }

    private sealed class Order : EntityBase<CustomerId>
    {
        public Order(CustomerId id)
            : base(id)
        {
        }
    }

    private sealed class Money : ValueObject
    {
        public Money(string currency, decimal amount)
        {
            Currency = currency;
            Amount = amount;
        }

        public string Currency { get; }

        public decimal Amount { get; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Currency;
            yield return Amount;
        }
    }

    private sealed class CustomerActivatedDomainEvent : DomainEvent
    {
        public CustomerActivatedDomainEvent(CustomerId customerId)
        {
            CustomerId = customerId;
        }

        public CustomerId CustomerId { get; }
    }
}
