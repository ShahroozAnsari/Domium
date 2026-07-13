# Domium Architecture

Domium is organized as small provider-oriented packages. The framework gives applications a
consistent domain and application model while keeping infrastructure choices explicit.

## Layers

```text
Domain
  Entities, aggregate roots, strongly-typed ids, value objects, domain events

Application
  Command/query buses and the behavior pipeline:
  Observability → Validation → Logging → Idempotency → Transaction → handler (commands)
  Observability → Validation → Logging → Caching → handler (queries)

Configuration
  DomiumOptions, feature toggles, and the AddDomium registration pipeline

Facade
  Module-level APIs (DomiumFacade / DomiumCommandFacade / DomiumQueryFacade)

Persistence
  Provider-neutral repository + unit-of-work + specification contracts
  EF Core provider (interceptors, tenant DbContext helpers, migrations helpers)
  Dapper provider

Infrastructure providers
  Memory cache, Redis cache, MassTransit, OpenTelemetry

Querying
  Attribute-allow-listed dynamic filtering/sorting/paging for read endpoints
```

## Design rules

- Domain packages never depend on EF Core, Dapper, Redis, MassTransit, or OpenTelemetry.
- Core registration lives in `Domium.Configuration`; `Domium.Extensions.DependencyInjection`
  only exposes `AddDomium`. The composition package references **no provider clients** —
  store selection happens through provider extension methods (`UseMemory()`, `UseRedis(...)`),
  so applications ship only the providers they use.
- `AddDomium` scans loaded non-framework assemblies for handlers by default; application
  services (repositories, read models) auto-register **only** when both the interface's and
  the implementation's assemblies were explicitly added via `AddApplicationAssembly` —
  modules cannot silently bind to each other's contracts.
- **One `DomiumDbContext` per process.** A service owns exactly one write model; the unit of
  work, repositories, and interceptors bind to it. Separate bounded contexts that need
  separate write models belong in separate services (databases follow the
  `{tenant}_{service}` convention per service).
- Domain events dispatch in-process in the same DI scope and transaction as the command;
  integration events go through MassTransit (configure its EF outbox for exactly-once).
- `IRepository<TAggregate, TId>` is for aggregate persistence only; read models query their
  own no-tracking DbContext.
- Query caching and idempotency share one `IDomiumCache` store; keys are namespaced per
  feature so they never collide.
