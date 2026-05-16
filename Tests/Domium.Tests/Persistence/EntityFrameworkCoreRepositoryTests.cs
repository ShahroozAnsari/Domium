using Domium.Domain;
using Domium.Domain.Abstractions.Events;
using Domium.Persistence.Abstractions;
using Domium.Persistence.Abstractions.Specifications;
using Domium.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Tests.Persistence;

public sealed class EntityFrameworkCoreRepositoryTests
{
    [Fact]
    public async Task Repository_adds_and_loads_aggregate_by_id()
    {
        await using var provider = CreateProvider();
        var repository = provider.GetRequiredService<IRepository<Customer, CustomerId>>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var id = new CustomerId(Guid.NewGuid());

        await unitOfWork.BeginAsync();
        await repository.AddAsync(new Customer(id, "Ada", true));
        await unitOfWork.CommitAsync();

        var loaded = await repository.GetByIdAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal("Ada", loaded.Name);
    }

    [Fact]
    public async Task Repository_applies_specifications()
    {
        await using var provider = CreateProvider();
        var repository = provider.GetRequiredService<IRepository<Customer, CustomerId>>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();

        await unitOfWork.BeginAsync();
        await repository.AddAsync(new Customer(new CustomerId(Guid.NewGuid()), "Ada", true));
        await repository.AddAsync(new Customer(new CustomerId(Guid.NewGuid()), "Grace", false));
        await unitOfWork.CommitAsync();

        var activeCustomers = await repository.FindAsync(new ActiveCustomersSpecification());
        var count = await repository.CountAsync(new ActiveCustomersSpecification());
        var any = await repository.AnyAsync(new ActiveCustomersSpecification());

        Assert.Single(activeCustomers);
        Assert.Equal("Ada", activeCustomers[0].Name);
        Assert.Equal(1, count);
        Assert.True(any);
    }

    [Fact]
    public async Task DbContext_dispatches_domain_events_after_save()
    {
        CapturingDispatcher.Reset();
        await using var provider = CreateProvider();
        var repository = provider.GetRequiredService<IRepository<Customer, CustomerId>>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var customer = new Customer(new CustomerId(Guid.NewGuid()), "Ada", true);

        customer.Activate();

        await unitOfWork.BeginAsync();
        await repository.AddAsync(customer);
        await unitOfWork.CommitAsync();

        Assert.IsType<CustomerActivatedDomainEvent>(Assert.Single(CapturingDispatcher.Events));
        Assert.Empty(customer.DomainEvents);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IDomainEventDispatcher, CapturingDispatcher>();
        services.AddDbContext<TestDbContext>(options =>
            options
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddDomiumEntityFrameworkCore<TestDbContext>();

        return services.BuildServiceProvider();
    }

    private sealed class TestDbContext(
        DbContextOptions<TestDbContext> options,
        IDomainEventDispatcher domainEventDispatcher)
        : DomiumDbContext(options, domainEventDispatcher)
    {
        public DbSet<Customer> Customers => Set<Customer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(builder =>
            {
                builder.HasKey(customer => customer.Id);
                builder.Property(customer => customer.Id)
                    .HasConversion(
                        id => id.Value,
                        value => new CustomerId(value));
                builder.Property(customer => customer.Name).IsRequired();
                builder.Ignore(customer => customer.DomainEvents);
            });
        }
    }

    private sealed class CustomerId(Guid value) : AggregateId<Guid>(value);

    private sealed class Customer : AggregateRoot<CustomerId>
    {
        private Customer()
            : base(new CustomerId(Guid.Empty))
        {
            Name = string.Empty;
        }

        public Customer(CustomerId id, string name, bool isActive)
            : base(id)
        {
            Name = name;
            IsActive = isActive;
        }

        public string Name { get; private set; }

        public bool IsActive { get; private set; }

        public void Activate()
        {
            IsActive = true;
            RaiseDomainEvent(new CustomerActivatedDomainEvent(Id));
        }
    }

    private sealed class CustomerActivatedDomainEvent(CustomerId customerId) : DomainEvent
    {
        public CustomerId CustomerId { get; } = customerId;
    }

    private sealed class ActiveCustomersSpecification : Specification<Customer>
    {
        public ActiveCustomersSpecification()
            : base(customer => customer.IsActive)
        {
            ApplyOrderBy(customer => customer.Name);
        }
    }

    private sealed class CapturingDispatcher : IDomainEventDispatcher
    {
        private static readonly List<IDomainEvent> CapturedEvents = new List<IDomainEvent>();

        public static IReadOnlyCollection<IDomainEvent> Events => CapturedEvents.AsReadOnly();

        public static void Reset()
        {
            CapturedEvents.Clear();
        }

        public Task DispatchAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default)
        {
            CapturedEvents.AddRange(domainEvents);
            return Task.CompletedTask;
        }
    }
}
