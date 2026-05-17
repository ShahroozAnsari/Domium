# Persistence

Domium persistence is intentionally provider-selectable. The core abstraction is small and aggregate-focused, while provider-specific packages expose the capabilities that only that provider can honestly support.

## Core Contract

`Domium.Persistence.Abstractions` contains:

```csharp
public interface IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TId>
    where TId : IAggregateId
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}
```

This is not a general querying abstraction. It is the command-side aggregate repository.

## EF Core Provider

Register EF Core with Domium:

```csharp
services.AddDomiumEntityFrameworkCore<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

services.AddDomium(options => options.UseTransactions());
```

`DomiumDbContext` provides:

- domain event collection and dispatch
- post-commit dispatch when used with `EfUnitOfWork`
- audit metadata updates for auditable entities
- soft delete conversion for soft-deletable entities

EF-specific query specifications are exposed through:

```csharp
IEfRepository<TAggregate, TId>
```

Example:

```csharp
public sealed class ActiveOrdersSpecification : Specification<Order>
{
    public ActiveOrdersSpecification()
        : base(order => order.IsActive)
    {
        ApplyOrderBy(order => order.Number);
    }
}
```

```csharp
var orders = await efRepository.FindAsync(
    new ActiveOrdersSpecification(),
    cancellationToken);
```

## Dapper Provider

Dapper registration starts with a connection factory:

```csharp
public sealed class SqlConnectionFactory(string connectionString)
    : IDapperConnectionFactory
{
    public ValueTask<DbConnection> CreateConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<DbConnection>(
            new SqlConnection(connectionString));
    }
}
```

```csharp
services.AddDomiumDapper(options =>
{
    options.UseConnectionFactory<SqlConnectionFactory>();
});
```

Use `IDapperSqlExecutor` for explicit SQL:

```csharp
var order = await sql.QuerySingleOrDefaultAsync<OrderReadModel>(
    "select Id, Number from Orders where Id = @Id",
    new { Id = query.Id },
    cancellationToken);
```

## Dapper Aggregate Repository

Dapper can also provide `IRepository<TAggregate, TId>`, but only when the application supplies explicit aggregate mapping.

```csharp
services.AddScoped<IDapperAggregateMapper<Order, OrderId>, OrderMapper>();

services.AddDomiumDapper(options =>
{
    options
        .UseConnectionFactory<SqlConnectionFactory>()
        .UseAggregateRepositories();
});
```

Mapper example:

```csharp
public sealed class OrderMapper : IDapperAggregateMapper<Order, OrderId>
{
    public string SelectByIdSql => "select Id, Number from Orders where Id = @Id";
    public string InsertSql => "insert into Orders (Id, Number) values (@Id, @Number)";
    public string UpdateSql => "update Orders set Number = @Number where Id = @Id";
    public string DeleteSql => "delete from Orders where Id = @Id";

    public object GetIdParameters(OrderId id) => new { Id = id.Value };
    public object GetInsertParameters(Order aggregate) => new { Id = aggregate.Id.Value, aggregate.Number };
    public object GetUpdateParameters(Order aggregate) => new { Id = aggregate.Id.Value, aggregate.Number };
    public object GetDeleteParameters(Order aggregate) => new { Id = aggregate.Id.Value };

    public Order Map(object row)
    {
        var values = (IDictionary<string, object>)row;
        return new Order(
            new OrderId((Guid)values["Id"]),
            Convert.ToString(values["Number"]) ?? string.Empty);
    }
}
```

This keeps Dapper explicit and avoids pretending it can infer aggregate graphs, relationships, and invariants automatically.

## Choosing EF Core, Dapper, Or Both

Use EF Core when:

- you want change tracking
- you want rich aggregate graph persistence
- you want LINQ/specification querying
- your write model maps naturally to EF

Use Dapper when:

- you want explicit SQL
- you want lightweight read models
- you want full control over aggregate persistence SQL
- your application already owns stored procedures or hand-tuned queries

Use both when:

- EF Core is your write model
- Dapper is your read model/query side
- or different bounded contexts have different persistence needs
