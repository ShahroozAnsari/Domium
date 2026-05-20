# Domium Tutorial

This tutorial builds a small Orders module with Domium. It shows the intended flow from domain model to command/query handlers, facade, persistence, query caching, idempotency, eventing, and observability.

## 1. Install Packages

Start with the core packages:

```powershell
dotnet add package Domium.Domain
dotnet add package Domium.Application
dotnet add package Domium.Configuration
dotnet add package Domium.Facade
dotnet add package Domium.Extensions.DependencyInjection
```

Add provider packages based on the infrastructure you need:

```powershell
dotnet add package Domium.Persistence.EntityFrameworkCore
dotnet add package Domium.Persistence.Dapper
dotnet add package Domium.Caching.Redis
dotnet add package Domium.Eventing.MassTransit
dotnet add package Domium.Observability.OpenTelemetry
```

Use `Domium.Caching.Redis` when either query caching or idempotency must work across multiple application instances.

## 2. Model The Domain

Keep domain code free of infrastructure. Aggregates raise domain events, but they do not know how events are dispatched or persisted.

```csharp
public sealed class OrderId(Guid value) : AggregateId<Guid>(value);

public sealed class Order : AggregateRoot<OrderId>
{
    private Order() : base(new OrderId(Guid.Empty))
    {
        Number = string.Empty;
    }

    public Order(OrderId id, string number) : base(id)
    {
        Number = string.IsNullOrWhiteSpace(number)
            ? throw new ArgumentException("Order number is required.", nameof(number))
            : number.Trim();

        RaiseDomainEvent(new OrderCreatedDomainEvent(id));
    }

    public string Number { get; private set; }
}

public sealed class OrderCreatedDomainEvent(OrderId orderId) : DomainEvent
{
    public OrderId OrderId { get; } = orderId;
}
```

## 3. Add Commands

Commands express intent and change state. Use `IIdempotentCommand` for commands that may be retried by clients, message brokers, or gateways.

```csharp
public sealed record CreateOrderCommand(
    string Number,
    string IdempotencyKey) : IIdempotentCommand;

public sealed class CreateOrderHandler(IRepository<Order, OrderId> repository)
    : ICommandHandler<CreateOrderCommand>
{
    public Task HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var order = new Order(new OrderId(Guid.NewGuid()), command.Number);
        return repository.AddAsync(order, cancellationToken);
    }
}
```

Idempotency behavior is applied before transactions. The first command with a key reserves the key atomically. Duplicate commands with the same command type and key do not run the handler. If the handler fails, the reservation is removed so retry is possible.

## 4. Add Queries

Queries return DTOs or read models. They should not load aggregates just to render a screen.

```csharp
public sealed record GetOrderQuery(Guid Id) : IQuery<OrderReadModel>;

public sealed record OrderReadModel(Guid Id, string Number);

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

## 5. Add A Facade

Facades are a module boundary. They let presentation or other modules depend on one class while CQRS stays inside the application layer.

```csharp
public interface IOrderFacade : IFacade
{
    Task CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<OrderReadModel> GetAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class OrderFacade(ICommandBus commandBus, IQueryBus queryBus)
    : DomiumFacade(commandBus, queryBus), IOrderFacade
{
    public Task CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            new CreateOrderCommand(request.Number, request.IdempotencyKey),
            cancellationToken);
    }

    public Task<OrderReadModel> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return QueryAsync<GetOrderQuery, OrderReadModel>(
            new GetOrderQuery(id),
            cancellationToken);
    }
}
```

## 6. Configure Domium

`AddDomium` scans loaded non-framework assemblies by default. Add an explicit assembly when handlers live in an assembly that has not been loaded yet.

```csharp
services.AddDomium(options =>
{
    options
        .AddApplicationAssembly(typeof(CreateOrderHandler).Assembly)
        .UseValidation()
        .UseLogging()
        .UseTransactions()
        .UseIdempotency(idempotency =>
        {
            idempotency.Store.UseRedis("localhost:6379");
            idempotency.Expiration = TimeSpan.FromHours(24);
            idempotency.RequireIdempotencyKey = false;
        })
        .UseCaching(cache =>
        {
            cache.Store.UseMemory();
            cache.DefaultExpiration = TimeSpan.FromMinutes(5);
        });
});
```

Use separate Redis connections when query caching and idempotency have different operational requirements:

```csharp
services.AddDomium(options =>
{
    options.UseCaching(cache =>
    {
        cache.Store.UseRedis(queryCacheRedis);
        cache.DefaultExpiration = TimeSpan.FromMinutes(5);
    });

    options.UseIdempotency(idempotency =>
    {
        idempotency.Store.UseRedis(idempotencyRedis);
        idempotency.Expiration = TimeSpan.FromHours(24);
    });
});
```

Use a connection factory when the application owns Redis connection lifecycle:

```csharp
services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString));

services.AddDomium(options =>
{
    options.UseIdempotency(idempotency =>
    {
        idempotency.Store.UseRedis(provider =>
            provider.GetRequiredService<IConnectionMultiplexer>());
    });
});
```

## 7. Configure Persistence

For EF Core aggregate persistence:

```csharp
services.AddDomiumEntityFrameworkCore<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

services.AddDomium(options => options.UseTransactions());
```

For Dapper read models:

```csharp
services.AddDomiumDapper(options =>
{
    options.UseConnectionFactory<SqlConnectionFactory>();
});
```

Use both when EF Core owns the write model and Dapper owns read-model SQL.

## 8. Register Query Cache Policies

Query caching is opt-in per query type.

```csharp
public sealed class OrderCachePolicies : IDomiumQueryCachePolicyProvider
{
    public DomiumQueryCachePolicy? GetPolicy(Type queryType)
    {
        if (queryType == typeof(GetOrderQuery))
        {
            return new DomiumQueryCachePolicy(
                typeof(GetOrderQuery),
                enabled: true,
                DomiumQueryCacheScopeMode.Global,
                absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(5),
                slidingExpiration: null,
                keyPrefix: "orders",
                cacheNullValues: false,
                invalidationMetadata: new DomiumCacheInvalidationMetadata(
                    tags: new[] { "orders" },
                    entityKeys: null,
                    group: "orders"));
        }

        return null;
    }
}
```

You can also register policies at startup through `IDomiumQueryCachePolicyRegistry`.

## 9. Idempotency Semantics

The idempotency pipeline behaves as follows:

- `IIdempotentCommand` commands require a non-empty `IdempotencyKey`.
- Non-idempotent commands pass through unless `RequireIdempotencyKey` is true.
- Duplicate keys skip the handler.
- Handler failure removes the reservation so retry can happen.
- Handler success keeps the reservation until expiration.
- A completion marker write failure does not clear the reservation, because the command may already be committed.

For distributed systems, use Redis for idempotency. In-memory idempotency is only correct for single-process applications and tests.

## 10. Eventing

Domain events are raised by aggregates and dispatched by the application/persistence integration. External events can be published through MassTransit:

```csharp
services.AddDomiumMassTransitEventing();

services.AddMassTransit(configurator =>
{
    configurator.AddDomiumExternalEventConsumer<OrderSubmitted>();
    configurator.UsingRabbitMq((context, cfg) => cfg.ConfigureEndpoints(context));
});
```

## 11. Observability

Domium emits activities and metrics through `Domium.Observability`. Register OpenTelemetry when the application needs exporters:

```csharp
services.AddDomiumOpenTelemetry(options =>
{
    options.ServiceName = "Orders.Api";
    options.Environment = "Production";
    options.Otlp.Enabled = true;
    options.Otlp.Endpoint = "http://localhost:4317";
});
```

## 12. Testing Checklist

For application code built on Domium, test these edges:

- command validation failures do not open transactions
- failed idempotent commands can be retried
- successful idempotent commands do not run twice for the same key
- query cache policies use the expected expiration and scope
- tenant-scoped cache policies fail when no tenant context exists
- facade methods map requests to the expected command or query
- persistence provider registration happens before `UseTransactions()`

## Production Checklist

- Use Redis idempotency for multi-instance deployments.
- Keep query caching and idempotency Redis stores separate when they have different retention, eviction, or availability requirements.
- Set idempotency expiration to the longest realistic client retry window.
- Keep idempotency keys stable and generated by the caller for retried operations.
- Do not use aggregate repositories for arbitrary read-model querying.
- Register explicit application assemblies when handlers are in lazily loaded modules.
