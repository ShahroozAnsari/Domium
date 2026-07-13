using Domium.Application.Abstractions.Events;
using Domium.Domain;
using Domium.Domain.Abstractions.DomainService;
using Domium.Domain.Abstractions.Entity;
using Domium.Eventing;
using Domium.Eventing.Abstractions;
using Domium.Persistence.Abstractions;
using Domium.Persistence.Abstractions.Specifications;
using Domium.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
        var repository = provider.GetRequiredService<ISpecificationRepository<Customer, CustomerId>>();
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
    public async Task Aggregate_with_injected_bus_publishes_domain_events_immediately()
    {
        CapturingEventBus.Reset();
        await using var provider = CreateProvider();
        var customer = new Customer(
            new CustomerId(Guid.NewGuid()),
            "Ada",
            true,
            provider.GetRequiredService<IEventBus>());

        customer.Activate();

        Assert.IsType<CustomerActivatedDomainEvent>(Assert.Single(CapturingEventBus.Events));
    }

    [Fact]
    public async Task Aggregate_publish_throws_when_event_bus_fails()
    {
        await using var provider = CreateProvider<FailingEventBus>();
        var customer = new Customer(
            new CustomerId(Guid.NewGuid()),
            "Ada",
            true,
            provider.GetRequiredService<IEventBus>());

        Assert.Throws<InvalidOperationException>(() => customer.Activate());
    }

    [Fact]
    public async Task Aggregate_created_without_bus_buffers_events_until_save()
    {
        var customer = new Customer(new CustomerId(Guid.NewGuid()), "Ada", true);

        customer.Activate();

        Assert.Single(customer.PendingDomainEvents);
    }

    [Fact]
    public async Task Buffered_domain_events_dispatch_before_save_in_same_transaction()
    {
        // Real in-memory bus + a handler that writes through the SAME DbContext: the
        // interceptor publishes the buffered event right before SaveChanges, so the
        // handler's audit row persists atomically with the aggregate.
        var services = new ServiceCollection();
        services.AddDomiumEventing();
        services.AddScoped<IDomainEventHandler<CustomerActivatedDomainEvent>, CustomerActivatedAuditHandler>();
        services.AddDomiumEntityFrameworkCore<TestDbContext>(options =>
            options
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        await using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService<IRepository<Customer, CustomerId>>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var customer = new Customer(new CustomerId(Guid.NewGuid()), "Ada", false);

        await unitOfWork.BeginAsync();
        await repository.AddAsync(customer);
        customer.Activate();
        await unitOfWork.CommitAsync();

        var dbContext = provider.GetRequiredService<TestDbContext>();
        var audit = await dbContext.CustomerActivationAudits.SingleAsync();

        Assert.Equal(customer.Id, audit.CustomerId);
        Assert.Empty(customer.PendingDomainEvents);
    }

    [Fact]
    public async Task Materialized_aggregate_receives_event_bus_and_domain_services()
    {
        CapturingEventBus.Reset();
        await using var provider = CreateProviderWithDomainServices();
        var dbContext = provider.GetRequiredService<TestDbContext>();
        var customerId = new CustomerId(Guid.NewGuid());

        dbContext.Customers.Add(new Customer(customerId, "Ada", false));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var customer = await dbContext.Customers.SingleAsync(x => x.Id == customerId);

        Assert.Equal("pricing", customer.DomainServiceName());

        customer.Activate();

        Assert.IsType<CustomerActivatedDomainEvent>(Assert.Single(CapturingEventBus.Events));
    }

    [Fact]
    public async Task DbContext_applies_audit_and_soft_delete_metadata()
    {
        await using var provider = CreateProvider(currentUserId: "operator");
        var dbContext = provider.GetRequiredService<TestDbContext>();
        var customer = new SoftDeletedCustomer(new CustomerId(Guid.NewGuid()), "Ada");

        dbContext.SoftDeletedCustomers.Add(customer);
        await dbContext.SaveChangesAsync();

        Assert.NotEqual(
            default,
            dbContext.Entry(customer).Property<DateTimeOffset>(DomiumShadowPropertyNames.CreatedAt).CurrentValue);

        dbContext.SoftDeletedCustomers.Remove(customer);
        await dbContext.SaveChangesAsync();

        Assert.True(dbContext.Entry(customer).Property<bool>(DomiumShadowPropertyNames.IsDeleted).CurrentValue);
        Assert.NotNull(
            dbContext.Entry(customer).Property<DateTimeOffset?>(DomiumShadowPropertyNames.DeletedAt).CurrentValue);
        Assert.Equal(
            "operator",
            dbContext.Entry(customer).Property<string?>(DomiumShadowPropertyNames.DeletedBy).CurrentValue);
        Assert.Equal(EntityState.Unchanged, dbContext.Entry(customer).State);
    }

    [Fact]
    public async Task BaseAggregateConfiguration_applies_shadow_properties_and_soft_delete_filter()
    {
        await using var provider = CreateProvider(currentUserId: "operator");
        var dbContext = provider.GetRequiredService<TestDbContext>();
        var customer = new ConfiguredCustomer(new CustomerId(Guid.NewGuid()), "Ada");

        dbContext.ConfiguredCustomers.Add(customer);
        await dbContext.SaveChangesAsync();

        Assert.NotEqual(
            default,
            dbContext.Entry(customer).Property<DateTimeOffset>(DomiumShadowPropertyNames.CreatedAt).CurrentValue);
        Assert.Equal(
            "operator",
            dbContext.Entry(customer).Property<string?>(DomiumShadowPropertyNames.CreatedBy).CurrentValue);

        dbContext.ConfiguredCustomers.Remove(customer);
        await dbContext.SaveChangesAsync();

        // Soft-deleted aggregates are invisible to normal queries…
        Assert.Empty(await dbContext.ConfiguredCustomers.ToListAsync());

        // …but still reachable when the filter is explicitly bypassed.
        Assert.Single(await dbContext.ConfiguredCustomers.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Concurrency_protected_aggregate_rejects_stale_writes()
    {
        await using var provider = CreateProvider();
        var dbContext = provider.GetRequiredService<TestDbContext>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var customer = new ConfiguredCustomer(new CustomerId(Guid.NewGuid()), "Ada");

        dbContext.ConfiguredCustomers.Add(customer);
        await dbContext.SaveChangesAsync();

        // First legitimate update bumps the version token from 0 to 1.
        dbContext.Entry(customer).Property(nameof(ConfiguredCustomer.Name)).CurrentValue = "Ada Lovelace";
        dbContext.Entry(customer).State = EntityState.Modified;
        await dbContext.SaveChangesAsync();

        // Simulate a stale writer that still holds version 0.
        dbContext.Entry(customer).Property<long>(DomiumShadowPropertyNames.Version).OriginalValue = 0;
        dbContext.Entry(customer).State = EntityState.Modified;

        await unitOfWork.BeginAsync();
        await Assert.ThrowsAsync<DomiumConcurrencyException>(() => unitOfWork.CommitAsync());
    }

    [Fact]
    public async Task UnitOfWork_ExecuteAsync_commits_the_unit_and_rolls_back_on_failure()
    {
        await using var provider = CreateProvider();
        var repository = provider.GetRequiredService<IRepository<Customer, CustomerId>>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var id = new CustomerId(Guid.NewGuid());

        await unitOfWork.ExecuteAsync(() => repository.AddAsync(new Customer(id, "Ada", true)));

        Assert.NotNull(await repository.GetByIdAsync(id));

        var failingId = new CustomerId(Guid.NewGuid());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            unitOfWork.ExecuteAsync(async () =>
            {
                await repository.AddAsync(new Customer(failingId, "Grace", true));
                throw new InvalidOperationException("Handler failed.");
            }));

        // The failed unit never reached SaveChanges, so nothing was persisted.
        Assert.Null(await repository.GetByIdAsync(failingId));
    }

    private static ServiceProvider CreateProvider()
    {
        return CreateProvider<CapturingEventBus>(currentUserId: null);
    }

    private static ServiceProvider CreateProvider<TEventBus>()
        where TEventBus : class, IEventBus
    {
        return CreateProvider<TEventBus>(currentUserId: null);
    }

    private static ServiceProvider CreateProvider(string? currentUserId)
    {
        return CreateProvider<CapturingEventBus>(currentUserId);
    }

    private static ServiceProvider CreateProvider<TEventBus>(string? currentUserId)
        where TEventBus : class, IEventBus
    {
        var services = new ServiceCollection();

        services.AddSingleton<IEventBus, TEventBus>();
        if (currentUserId is not null)
        {
            services.AddScoped<IDomiumCurrentUserAccessor>(_ => new TestCurrentUserAccessor(currentUserId));
        }

        services.AddDomiumEntityFrameworkCore<TestDbContext>(options =>
            options
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateProviderWithDomainServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IEventBus, CapturingEventBus>();
        services.AddScoped<IDomainService, TestDomainService>();
        services.AddDomiumEntityFrameworkCore<TestDbContext>(options =>
            options
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        return services.BuildServiceProvider();
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DomiumDbContext(options)
    {
        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<SoftDeletedCustomer> SoftDeletedCustomers => Set<SoftDeletedCustomer>();

        public DbSet<ConfiguredCustomer> ConfiguredCustomers => Set<ConfiguredCustomer>();

        public DbSet<CustomerActivationAudit> CustomerActivationAudits => Set<CustomerActivationAudit>();

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
            });

            modelBuilder.Entity<SoftDeletedCustomer>(builder =>
            {
                builder.HasKey(customer => customer.Id);
                builder.Property(customer => customer.Id)
                    .HasConversion(
                        id => id.Value,
                        value => new CustomerId(value));
                builder.Property(customer => customer.Name).IsRequired();
                builder.Property<DateTimeOffset>(DomiumShadowPropertyNames.CreatedAt);
                builder.Property<DateTimeOffset?>(DomiumShadowPropertyNames.ModifiedAt);
                builder.Property<string?>(DomiumShadowPropertyNames.CreatedBy).HasMaxLength(160);
                builder.Property<string?>(DomiumShadowPropertyNames.ModifiedBy).HasMaxLength(160);
                builder.Property<bool>(DomiumShadowPropertyNames.IsDeleted);
                builder.Property<DateTimeOffset?>(DomiumShadowPropertyNames.DeletedAt);
                builder.Property<string?>(DomiumShadowPropertyNames.DeletedBy).HasMaxLength(160);
            });

            modelBuilder.ApplyConfiguration(new ConfiguredCustomerConfiguration());

            modelBuilder.Entity<CustomerActivationAudit>(builder =>
            {
                builder.HasKey(audit => audit.Id);
                builder.Property(audit => audit.CustomerId)
                    .HasConversion(
                        id => id.Value,
                        value => new CustomerId(value));
            });
        }
    }

    private sealed class CustomerId(Guid value) : AggregateId<Guid>(value);

    private sealed class Customer : AggregateRoot<CustomerId>
    {
        private Customer()
        {
            Name = string.Empty;
        }

        public Customer(CustomerId id, string name, bool isActive)
            : base(id)
        {
            Name = name;
            IsActive = isActive;
        }

        public Customer(CustomerId id, string name, bool isActive, IEventBus eventBus)
            : base(id, eventBus)
        {
            Name = name;
            IsActive = isActive;
        }

        private TestDomainService PricingService { get; set; } = default!;

        public string Name { get; private set; }

        public bool IsActive { get; private set; }

        public void Activate()
        {
            IsActive = true;
            RaiseEvent(new CustomerActivatedDomainEvent(Id));
        }

        public string DomainServiceName() =>
            PricingService.Name;
    }

    private sealed class CustomerActivatedDomainEvent(CustomerId customerId) : DomainEvent
    {
        public CustomerId CustomerId { get; } = customerId;
    }

    private sealed class CustomerActivationAudit
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public CustomerId CustomerId { get; private set; } = new(Guid.NewGuid());

        public static CustomerActivationAudit Create(CustomerId customerId) =>
            new() { CustomerId = customerId };
    }

    private sealed class CustomerActivatedAuditHandler(TestDbContext dbContext)
        : IDomainEventHandler<CustomerActivatedDomainEvent>
    {
        public Task HandleAsync(
            CustomerActivatedDomainEvent domainEvent,
            CancellationToken cancellationToken = default)
        {
            dbContext.CustomerActivationAudits.Add(CustomerActivationAudit.Create(domainEvent.CustomerId));
            return Task.CompletedTask;
        }
    }

    private sealed class TestDomainService : IDomainService
    {
        public string Name => "pricing";
    }

    private sealed class SoftDeletedCustomer : AggregateRoot<CustomerId>, IAuditableEntity, ISoftDeletableEntity
    {
        private SoftDeletedCustomer()
        {
            Name = string.Empty;
        }

        public SoftDeletedCustomer(CustomerId id, string name)
            : base(id)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

    private sealed class ConfiguredCustomer : AggregateRoot<CustomerId>, IAuditableEntity, ISoftDeletableEntity, IConcurrencyProtectedEntity
    {
        private ConfiguredCustomer()
        {
            Name = string.Empty;
        }

        public ConfiguredCustomer(CustomerId id, string name)
            : base(id)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

    private sealed class ConfiguredCustomerConfiguration : BaseAggregateConfiguration<ConfiguredCustomer>
    {
        protected override string TableName => "configured_customers";

        protected override string Schema => "test";

        protected override void ConfigureAggregate(EntityTypeBuilder<ConfiguredCustomer> builder)
        {
            builder.Property(customer => customer.Name).IsRequired();
        }
    }

    private sealed class TestCurrentUserAccessor(string? userId) : IDomiumCurrentUserAccessor
    {
        public string? UserId { get; } = userId;
    }

    private sealed class ActiveCustomersSpecification : Specification<Customer>
    {
        public ActiveCustomersSpecification()
            : base(customer => customer.IsActive)
        {
            ApplyOrderBy(customer => customer.Name);
        }
    }

    private sealed class CapturingEventBus : IEventBus
    {
        private static readonly List<IDomiumEvent> CapturedEvents = new();

        public static IReadOnlyCollection<IDomiumEvent> Events => CapturedEvents.AsReadOnly();

        public static void Reset()
        {
            CapturedEvents.Clear();
        }

        public Task PublishAsync<TEvent>(
            TEvent @event,
            CancellationToken cancellationToken = default)
            where TEvent : IDomiumEvent
        {
            CapturedEvents.Add(@event);
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

    private sealed class FailingEventBus : IEventBus
    {
        public Task PublishAsync<TEvent>(
            TEvent @event,
            CancellationToken cancellationToken = default)
            where TEvent : IDomiumEvent
        {
            throw new InvalidOperationException("Dispatch failed.");
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
