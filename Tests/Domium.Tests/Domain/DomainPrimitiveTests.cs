using Domium.Domain;
using Domium.Domain.Abstractions.Events;
using Domium.Eventing.Abstractions;

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
    public void Value_object_equality_uses_concrete_type()
    {
        var money = new Money("USD", 12.5m);
        var price = new Price("USD", 12.5m);

        Assert.NotEqual<ValueObject>(money, price);
    }

    [Fact]
    public void Value_object_equality_compares_value_object_lists_by_items()
    {
        var first = new MoneyBag(
        [
            new Money("USD", 12.5m),
            new Money("EUR", 20m)
        ]);
        var second = new MoneyBag(
        [
            new Money("USD", 12.5m),
            new Money("EUR", 20m)
        ]);
        var differentOrder = new MoneyBag(
        [
            new Money("EUR", 20m),
            new Money("USD", 12.5m)
        ]);

        Assert.Equal(first, second);
        Assert.NotEqual(first, differentOrder);
    }

    [Fact]
    public void Aggregate_root_publishes_domain_events_through_event_bus()
    {
        var eventBus = new CapturingEventBus();
        var customer = new Customer(new CustomerId(Guid.NewGuid()), eventBus);

        customer.Activate();

        var domainEvent = Assert.Single(eventBus.Events);
        Assert.IsType<CustomerActivatedDomainEvent>(domainEvent);
    }

    private sealed class CustomerId(Guid value) : AggregateId<Guid>(value);

    private sealed class Customer : AggregateRoot<CustomerId>
    {
        public Customer(CustomerId id)
            : base(id)
        {
        }

        public Customer(CustomerId id, IEventBus eventBus)
            : base(id, eventBus)
        {
        }

        public void Activate()
        {
            RaiseEvent(new CustomerActivatedDomainEvent(Id));
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

    private sealed class Price(string currency, decimal amount) : ValueObject
    {
        public string Currency { get; } = currency;

        public decimal Amount { get; } = amount;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Currency;
            yield return Amount;
        }
    }

    private sealed class MoneyBag(IEnumerable<Money> items) : ValueObject
    {
        private readonly IReadOnlyCollection<Money> _items = items.ToArray();

        public IReadOnlyCollection<Money> Items => _items;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            foreach (var item in Items)
            {
                yield return item;
            }
        }
    }

    private sealed class CustomerActivatedDomainEvent(CustomerId customerId) : DomainEvent
    {
        public CustomerId CustomerId { get; } = customerId;
    }

    private sealed class CapturingEventBus : IEventBus
    {
        private readonly List<IDomiumEvent> _events = new();

        public IReadOnlyCollection<IDomiumEvent> Events => _events.AsReadOnly();

        public Task PublishAsync<TEvent>(
            TEvent @event,
            CancellationToken cancellationToken = default)
            where TEvent : IDomiumEvent
        {
            _events.Add(@event);
            return Task.CompletedTask;
        }

        public async Task PublishAsync(
            IReadOnlyCollection<IDomiumEvent> events,
            CancellationToken cancellationToken = default)
        {
            foreach (var @event in events)
            {
                await PublishAsync(@event, cancellationToken);
            }
        }
    }
}
