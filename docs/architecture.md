# Domium Architecture

Domium is organized as small provider-oriented packages. The framework gives applications a consistent domain and application model while allowing infrastructure choices to stay explicit.

## Layers

```text
Domain
  Entities, aggregate roots, IDs, value objects, domain events

Application
  Commands, queries, handlers, validation, logging, transactions, caching

Persistence
  Provider-neutral aggregate repository contract
  EF Core provider
  Dapper provider

Infrastructure Providers
  Memory cache, Redis cache, MassTransit, OpenTelemetry

Composition
  AddDomium and provider-specific registration methods
```

## Design Rules

- Domain packages do not depend on EF Core, Dapper, Redis, MassTransit, or OpenTelemetry.
- `IRepository<TAggregate, TId>` is for aggregate persistence only.
- Query/read-model infrastructure is intentionally separate from aggregate persistence.
- EF-specific specification querying lives in the EF Core package through `IEfRepository<TAggregate, TId>`.
- Dapper aggregate persistence is opt-in and requires explicit mappers.
- Provider packages register their own infrastructure so `Domium.Extensions.DependencyInjection` stays lightweight.

## Command Side

The command side loads and saves aggregates:

```csharp
public sealed class CreateOrderHandler(IRepository<Order, OrderId> repository)
    : ICommandHandler<CreateOrderCommand>
{
    public async Task HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var order = new Order(new OrderId(Guid.NewGuid()), command.Number);
        await repository.AddAsync(order, cancellationToken);
    }
}
```

Transactions are enabled by adding a persistence provider and then enabling the transaction pipeline:

```csharp
services.AddDomiumEntityFrameworkCore<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

services.AddDomium(options => options.UseTransactions());
```

## Query Side

Queries return DTOs/read models. They can use EF Core, Dapper, direct SQL, external APIs, or any application-owned data access strategy:

```csharp
public sealed class GetOrderHandler(IDapperSqlExecutor sql)
    : IQueryHandler<GetOrderQuery, OrderReadModel>
{
    public Task<OrderReadModel> HandleAsync(
        GetOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        return sql.QuerySingleAsync<OrderReadModel>(
            "select Id, Number from Orders where Id = @Id",
            new { query.Id },
            cancellationToken);
    }
}
```

## Domain Events

Aggregates raise domain events. `DomiumDbContext` collects and dispatches them after persistence succeeds. When using `EfUnitOfWork`, events are dispatched after the transaction commits.

This protects the domain model from losing events on failed saves and prevents handlers from observing uncommitted EF data.

## Provider Selection

Applications can choose one or multiple providers:

- EF Core for aggregate persistence and specifications.
- Dapper for read models and/or mapped aggregate persistence.
- Memory or Redis for query caching.
- MassTransit for external events.
- OpenTelemetry for observability.

The framework does not force read models into the domain repository. That keeps CQRS boundaries clear while still allowing Dapper as an aggregate persistence provider when the application explicitly maps aggregates.
