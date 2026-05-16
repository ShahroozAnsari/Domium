# Domium

Domium is a lightweight DDD/CQRS foundation for .NET applications. It provides domain primitives, command and query pipelines, persistence contracts, tenant-aware query caching, and dependency injection registration.

## Projects

- `Domain/Domium.Domain.Abstractions`: DDD contracts for entities, aggregate roots, value objects, and domain events.
- `Domain/Domium.Domain`: concrete base types such as `EntityBase<TId>`, `AggregateRoot<TId>`, `ValueObject`, `AggregateId<T>`, and `DomainEvent`.
- `Application/Domium.Application.Abstractions`: command/query buses, handlers, validators, and pipeline contracts.
- `Application/Domium.Application`: command/query bus implementations, pipeline behaviors, and domain event dispatching.
- `Persistence/Domium.Persistence.Abstractions`: repository, unit-of-work, and specification contracts.
- `Persistence/Domium.Persistence.EntityFrameworkCore`: EF Core repository, unit-of-work, specification evaluator, and DbContext base.
- `Caching/*`: cache policy abstractions and memory/Redis cache stores.
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
        .UseTransactions()
        .UseCaching(cache =>
        {
            cache.Provider = DomiumCacheProvider.Memory;
            cache.DefaultExpiration = TimeSpan.FromMinutes(5);
        });
});
```

For EF Core persistence:

```csharp
services.AddDbContext<AppDbContext>(options => /* configure provider */);
services.AddDomiumEntityFrameworkCore<AppDbContext>();
```

For ambient tenant scopes:

```csharp
using var scope = tenantScopeFactory.BeginScope("tenant-42");
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
