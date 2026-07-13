# Persistence

Domium persistence is provider-selectable. The core contract is small and aggregate-focused;
provider packages expose only what that provider can honestly support.

## Core contracts

`Domium.Persistence.Abstractions` contains:

```csharp
public interface IRepository<TAggregate, in TId>
    where TAggregate : class, IAggregateRoot<TId>
    where TId : IAggregateId
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}
```

LINQ-capable providers add specification-based reads:

```csharp
public interface ISpecificationRepository<TAggregate, in TId> : IRepository<TAggregate, TId>
{
    Task<IReadOnlyList<TAggregate>> FindAsync(ISpecification<TAggregate> specification, ...);
    Task<int> CountAsync(ISpecification<TAggregate> specification, ...);
    Task<bool> AnyAsync(ISpecification<TAggregate> specification, ...);
}
```

This is the command-side aggregate repository — not a general querying abstraction. Read
models query their own (no-tracking) DbContext directly.

`IUnitOfWork` exposes `Begin/Commit/Rollback` plus `ExecuteAsync(operation)`, which runs the
whole unit through the provider's execution strategy — use it (the built-in
TransactionCommandBehavior does) so `EnableRetryOnFailure` works. Begin/Commit pairs may
nest; only the outermost pair commits.

## EF Core provider

```csharp
services.AddDomiumEntityFrameworkCore<OrdersDbContext>(options => options.UseNpgsql(connectionString));
```

That single call registers the context as `DomiumDbContext`, the unit of work, the generic
repositories, and the interceptors below.

### Model discovery is explicit

A `DomiumDbContext` declares which assemblies hold its entity configurations:

```csharp
public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DomiumDbContext(options)
{
    protected override IEnumerable<Assembly> GetConfigurationAssemblies()
    {
        yield return typeof(OrdersInfrastructureMarker).Assembly;
    }
}
```

Nothing outside those assemblies enters the model — bounded contexts stay isolated, and the
model does not depend on assembly load order.

### What the interceptors do on SaveChanges

| Concern | Behavior |
| --- | --- |
| Domain events | Buffered events from tracked aggregates are published **before** the save, in the same scope and transaction; handler changes persist atomically with the aggregate. Handlers must not call SaveChanges (enforced). |
| Auditing | `IAuditableEntity` gets CreatedAt/By and ModifiedAt/By shadow columns. |
| Soft delete | `ISoftDeletableEntity` deletes become updates (IsDeleted/DeletedAt/DeletedBy) and a global query filter hides deleted rows (`IgnoreQueryFilters()` to opt out). |
| Optimistic concurrency | `IConcurrencyProtectedEntity` gets a Version concurrency token, bumped on every update/delete; stale writers get `DomiumConcurrencyException`. |
| Domain services / event bus | Materialized aggregates receive `IEventBus` and `IDomainService` instances via property injection. |

### Mapping

Derive from `BaseAggregateConfiguration<T>`: it maps table/schema, converts strongly-typed
Guid ids (compiled, no per-row reflection), and adds the shadow columns and query filter for
the markers above.

### Migrations for tenant-per-database

`EnsureCreated` can only create a schema — it can never upgrade one. Author EF migrations
(each persistence project ships an `IDesignTimeDbContextFactory`), then:

- **Provisioning:** `DomiumTenantMigrations.MigrateOrCreateAsync(dbContext)` applies
  migrations when they exist and falls back to EnsureCreated while none are authored.
- **Deploys:** loop every tenant database with
  `DomiumTenantMigrations.MigrateTenantsAsync(tenantIds, tenantId => CreateContextFor(tenantId))`,
  resolving each connection via `IDomiumTenantConnectionResolver.ResolveFor(...)`.

## Dapper provider

`Domium.Persistence.Dapper` implements the same `IRepository` core over explicit SQL
mappers, plus a session/unit-of-work (with the same nesting semantics) for hand-written SQL.

## Multi-tenant registration

```csharp
services.AddDomiumTenantDbContext<OrdersDbContext>(
    "orders", baseConnectionString, (options, cs) => options.UseNpgsql(cs));
```

The connection is resolved per request from the ambient tenant and the
`{tenant}_{service}` naming convention. One `DomiumDbContext` per process is the design
principle — a service owns one write model; separate services own separate databases.
