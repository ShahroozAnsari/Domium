# Domium

Domium is a lightweight DDD/CQRS foundation for .NET applications. It provides domain primitives, command and query pipelines, persistence contracts, tenant-aware query caching, and dependency injection registration.

## Projects

- `Domain/Domium.Domain.Abstractions`: DDD contracts for entities, aggregate roots, value objects, and domain events.
- `Domain/Domium.Domain`: concrete base types such as `EntityBase<TId>`, `AggregateRoot<TId>`, `ValueObject`, `AggregateId<T>`, and `DomainEvent`.
- `Application/Domium.Application.Abstractions`: command/query buses, handlers, validators, and pipeline contracts.
- `Application/Domium.Application`: command/query bus implementations, pipeline behaviors, and domain event dispatching.
- `Persistence/Domium.Persistence.Abstractions`: aggregate repository and unit-of-work contracts.
- `Persistence/Domium.Persistence.EntityFrameworkCore`: EF Core repository, unit-of-work, specification evaluator, and DbContext base.
- `Persistence/Domium.Persistence.Dapper`: Dapper connection/session, unit-of-work, SQL executor, and optional mapped aggregate repository.
- `Caching/*`: cache policy abstractions and memory/Redis cache stores.
- `Eventing/Domium.Eventing.Abstractions`: provider-neutral internal/external event contracts.
- `Eventing/Domium.Eventing`: in-process internal event publishing and default external event no-op publisher.
- `Eventing/Domium.Eventing.MassTransit`: MassTransit external event publisher and consumer adapter.
- `Tenancy/Domium.Tenancy.Abstractions`: tenant context access used by tenant-scoped caching.
- `Tenancy/Domium.Tenancy`: AsyncLocal tenant context and disposable tenant scopes.
- `Extensions/Domium.Extensions.DependencyInjection`: the `AddDomium` registration entry point.

## Basic Usage

```csharp
services.AddDomium(options =>
{
    options
        .UseValidation()
        .UseLogging()
        .UseCaching(cache =>
        {
            cache.Provider = DomiumCacheProvider.Memory;
            cache.DefaultExpiration = TimeSpan.FromMinutes(5);
        });
});
```

For EF Core persistence:

```csharp
services.AddDomiumEntityFrameworkCore<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

services.AddDomium(options => options.UseTransactions());
```

For Dapper persistence:

```csharp
services.AddDomiumDapper(options =>
{
    options.UseConnectionFactory<SqlConnectionFactory>();
});

services.AddDomium(options => options.UseTransactions());
```

Dapper can be used for explicit SQL:

```csharp
var orders = await sql.QueryAsync<OrderReadModel>(
    "select Id, Number from Orders where TenantId = @TenantId",
    new { TenantId = tenantId },
    cancellationToken);
```

Or it can be selected as the aggregate repository provider when the application supplies aggregate mappings:

```csharp
services.AddScoped<IDapperAggregateMapper<Order, OrderId>, OrderMapper>();

services.AddDomiumDapper(options =>
{
    options
        .UseConnectionFactory<SqlConnectionFactory>()
        .UseAggregateRepositories();
});
```

EF-specific specification queries are available through `IEfRepository<TAggregate, TId>`. The core `IRepository<TAggregate, TId>` intentionally only represents aggregate load/save behavior.

For Redis-backed query caching:

```csharp
services.AddDomiumRedisCacheStore("localhost");
services.AddDomium(options =>
{
    options.UseCaching(cache => cache.Provider = DomiumCacheProvider.Redis);
});
```

When handlers live outside the assembly that calls `AddDomium`, register those assemblies explicitly:

```csharp
services.AddDomium(options =>
{
    options.AddApplicationAssembly(typeof(CreateOrderHandler).Assembly);
});
```

For ambient tenant scopes:

```csharp
using var scope = tenantScopeFactory.BeginScope("tenant-42");
```

For external events with MassTransit:

```csharp
services.AddDomiumMassTransitEventing();
services.AddMassTransit(configurator =>
{
    configurator.AddDomiumExternalEventConsumer<OrderSubmitted>();
    configurator.UsingRabbitMq((context, cfg) => cfg.ConfigureEndpoints(context));
});
```

Application assemblies can then define:

- `ICommandHandler<TCommand>`
- `IQueryHandler<TQuery, TResult>`
- `ICommandValidator<TCommand>`
- `IQueryValidator<TQuery, TResult>`
- `IDomainEventHandler<TDomainEvent>`
- optional `IDomiumQueryCachePolicyProvider`

## Verification

Run the full solution with:

```powershell
dotnet test Domium.slnx
```
