using Domium.Domain;
using Domium.Domain.Abstractions.Aggregate;
using Domium.Domain.Abstractions.Entity;
using Domium.Domain.Abstractions.Events;
using Domium.Persistence.Abstractions;
using Domium.Persistence.EntityFrameworkCore;
using Domium.Persistence.EntityFrameworkCore.Specifications;
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
    public async Task EntityFrameworkCore_registration_can_configure_dbcontext()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IDomainEventDispatcher, CapturingDispatcher>();
        services.AddDomiumEntityFrameworkCore<TestDbContext>(options =>
            options
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        await using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService<IEfRepository<Customer, CustomerId>>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var id = new CustomerId(Guid.NewGuid());

        await unitOfWork.BeginAsync();
        await repository.AddAsync(new Customer(id, "Ada", true));
        await unitOfWork.CommitAsync();

        Assert.NotNull(await repository.GetByIdAsync(id));
    }

    [Fact]
    public async Task Repository_applies_specifications()
    {
        await using var provider = CreateProvider();
        var repository = provider.GetRequiredService<IEfRepository<Customer, CustomerId>>();
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
        var repository = provider.GetRequiredService<IEfRepository<Customer, CustomerId>>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var customer = new Customer(new CustomerId(Guid.NewGuid()), "Ada", true);

        customer.Activate();

        await unitOfWork.BeginAsync();
        await repository.AddAsync(customer);
        await unitOfWork.CommitAsync();

        Assert.IsType<CustomerActivatedDomainEvent>(Assert.Single(CapturingDispatcher.Events));
        Assert.Empty(customer.DomainEvents);
    }

    [Fact]
    public async Task DbContext_keeps_domain_events_when_dispatch_fails()
    {
        await using var provider = CreateProvider<FailingDispatcher>();
        var repository = provider.GetRequiredService<IEfRepository<Customer, CustomerId>>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var customer = new Customer(new CustomerId(Guid.NewGuid()), "Ada", true);

        customer.Activate();

        await unitOfWork.BeginAsync();
        await repository.AddAsync(customer);

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.CommitAsync());

        Assert.NotEmpty(customer.DomainEvents);
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
        Assert.NotNull(
            dbContext.Entry(customer).Property<DateTimeOffset?>(DomiumShadowPropertyNames.ModifiedAt).CurrentValue);
        Assert.Equal(EntityState.Unchanged, dbContext.Entry(customer).State);
    }

    [Fact]
    public async Task BaseEntityConfiguration_applies_shadow_properties_to_owned_children()
    {
        await using var provider = CreateProvider(currentUserId: "operator");
        var dbContext = provider.GetRequiredService<TestDbContext>();
        var customer = new ConfiguredCustomer(new CustomerId(Guid.NewGuid()), "Ada");
        var note = new ConfiguredCustomerNote(Guid.NewGuid(), "Call before noon");

        customer.AddNote(note);
        dbContext.ConfiguredCustomers.Add(customer);
        await dbContext.SaveChangesAsync();

        var noteEntry = dbContext.Entry(note);
        Assert.NotEqual(
            default,
            noteEntry.Property<DateTimeOffset>(DomiumShadowPropertyNames.CreatedAt).CurrentValue);
        Assert.Equal(
            "operator",
            noteEntry.Property<string?>(DomiumShadowPropertyNames.CreatedBy).CurrentValue);

        noteEntry.State = EntityState.Deleted;
        await dbContext.SaveChangesAsync();

        Assert.True(noteEntry.Property<bool>(DomiumShadowPropertyNames.IsDeleted).CurrentValue);
        Assert.NotNull(noteEntry.Property<DateTimeOffset?>(DomiumShadowPropertyNames.DeletedAt).CurrentValue);
        Assert.NotNull(noteEntry.Property<DateTimeOffset?>(DomiumShadowPropertyNames.ModifiedAt).CurrentValue);
        Assert.Equal(
            "operator",
            noteEntry.Property<string?>(DomiumShadowPropertyNames.DeletedBy).CurrentValue);
    }

    private static ServiceProvider CreateProvider()
    {
        return CreateProvider<CapturingDispatcher>();
    }

    private static ServiceProvider CreateProvider<TDispatcher>()
        where TDispatcher : class, IDomainEventDispatcher
    {
        return CreateProvider<TDispatcher>(currentUserId: null);
    }

    private static ServiceProvider CreateProvider(string? currentUserId)
    {
        return CreateProvider<CapturingDispatcher>(currentUserId);
    }

    private static ServiceProvider CreateProvider<TDispatcher>(string? currentUserId)
        where TDispatcher : class, IDomainEventDispatcher
    {
        var services = new ServiceCollection();

        services.AddSingleton<IDomainEventDispatcher, TDispatcher>();
        if (currentUserId is not null)
        {
            services.AddScoped<IDomiumCurrentUserAccessor>(_ => new TestCurrentUserAccessor(currentUserId));
        }

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

        public DbSet<SoftDeletedCustomer> SoftDeletedCustomers => Set<SoftDeletedCustomer>();

        public DbSet<ConfiguredCustomer> ConfiguredCustomers => Set<ConfiguredCustomer>();

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
                builder.Ignore(customer => customer.DomainEvents);
            });

            modelBuilder.ApplyConfiguration(new ConfiguredCustomerConfiguration());
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

    private sealed class ConfiguredCustomer : AggregateRoot<CustomerId>
    {
        private readonly List<ConfiguredCustomerNote> _notes = new List<ConfiguredCustomerNote>();

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

        public IReadOnlyCollection<ConfiguredCustomerNote> Notes => _notes.AsReadOnly();

        public void AddNote(ConfiguredCustomerNote note)
        {
            _notes.Add(note);
        }
    }

    private sealed class ConfiguredCustomerNote : EntityBase<Guid>, IAuditableEntity, ISoftDeletableEntity
    {
        private ConfiguredCustomerNote()
            : base(Guid.Empty)
        {
            Text = string.Empty;
        }

        public ConfiguredCustomerNote(Guid id, string text)
            : base(id)
        {
            Text = text;
        }

        public string Text { get; private set; }
    }

    private sealed class ConfiguredCustomerConfiguration : BaseEntityConfiguration<ConfiguredCustomer>
    {
        protected override string TableName => "configured_customers";

        protected override string Schema => "test";

        protected override void ConfigureAggregate(EntityTypeBuilder<ConfiguredCustomer> builder)
        {
            builder.Property(customer => customer.Id)
                .HasConversion(
                    id => id.Value,
                    value => new CustomerId(value));
            builder.Property(customer => customer.Name).IsRequired();
            builder.OwnsMany(customer => customer.Notes, noteBuilder =>
            {
                noteBuilder.ToTable("configured_customer_notes", "test");
                noteBuilder.WithOwner().HasForeignKey("CustomerId");
                noteBuilder.HasKey(note => note.Id);
                noteBuilder.Property(note => note.Text).IsRequired();
            });
            builder.Navigation(customer => customer.Notes).UsePropertyAccessMode(PropertyAccessMode.Field);
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

    private sealed class FailingDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Dispatch failed.");
        }
    }
}
