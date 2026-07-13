# Changelog

Domium follows [SemVer](https://semver.org). Until 1.0.0, minor versions may contain
breaking changes; each one is listed here.

## [0.2.0] — Unreleased

The "make it trustworthy" release: the event/transaction core, repositories, caching, and
tenancy were reworked, and the test suite was rebuilt against the new surface.

### Breaking changes

- **Caching rebuilt.** The policy/scope/key-provider model and the `Domium.Caching` package
  are gone. One store interface remains — `IDomiumCache` (Get/Set/TrySet/Remove/RemoveByTag)
  — with `Domium.Caching.Memory` and `Domium.Caching.Redis` providers. Queries opt in via
  `ICacheableQuery` (duration + invalidation tags). Store selection moved to provider
  extension methods: `Store.UseMemory()` (Memory package) / `Store.UseRedis(...)` (Redis
  package); the composition package no longer references the Redis client.
- **Tenancy renamed for clarity.** `IDomiumTenantNameResolver.ResolveTenantName()` →
  `IDomiumTenantResolver.ResolveTenantId()`; `IDomiumTenantConnectionStringResolver` →
  `IDomiumTenantConnectionResolver` with ambient `Resolve(service, template)` and explicit
  `ResolveFor(tenantId, service, template)`. New one-line registration:
  `AddDomiumTenantDbContext<TContext>(service, template, provider)`.
- **Repository is a real contract.** `IRepository<TAggregate,TId>` (GetById/Add/Update/
  Remove) plus `ISpecificationRepository<TAggregate,TId>` (Find/Count/Any over
  specifications) in `Domium.Persistence.Abstractions`; `EfRepository` implements both.
- **Query results unconstrained.** `IQuery<TResult>` no longer requires `class` — nullable
  and value-type results are first-class (removes the CS8634 workarounds).
- **`DomiumDbContext` model discovery is explicit.** Override `GetConfigurationAssemblies()`;
  the AppDomain-wide scan is gone (bounded contexts no longer leak into each other's models).
- **DI convention tightened.** Application services auto-register only when both the
  interface's and the implementation's assemblies were explicitly added via
  `AddApplicationAssembly` (namespace-root matching removed).
- **Renames:** `BaseAgregateConfiguration` → `BaseAggregateConfiguration`;
  `DomainException` now derives from `Exception`; `IIdempotentCommand.IdempotencyKey` is
  get-only; `ICommandBus` token parameter is `cancellationToken`.

### Added

- `ICommand<TResult>` end-to-end (bus, handler, pipeline behaviors) — commands can return
  the created id.
- Observability as a pipeline behavior (`UseObservability()`), outermost around
  validation/logging/idempotency/transaction; buses are now plain dispatchers.
- Buffered domain-event dispatch: aggregates created with `new` buffer events;
  `DomainEventDispatchInterceptor` publishes them right before SaveChanges in the same
  scope/transaction (with a guard that rejects SaveChanges calls from inside handlers).
- Optimistic concurrency: mark an aggregate `IConcurrencyProtectedEntity` to get a version
  concurrency token; stale writes surface as `DomiumConcurrencyException`.
- `IUnitOfWork.ExecuteAsync(operation)` — runs the unit through the EF execution strategy,
  compatible with `EnableRetryOnFailure`; nested Begin/Commit pairs ref-count (EF and Dapper).
- Migrations story for tenant-per-database: `DomiumTenantMigrations.MigrateOrCreateAsync` and
  `MigrateTenantsAsync` (deploy-time upgrade loop over all tenant databases).
- Dynamic querying hardening: page size clamped (default max 200), filter conditions capped
  (32), sort keys capped (8); `Guid`/enum/`DateTimeOffset`/`TimeSpan` filter values parse
  correctly; multi-key sort (`"Name,-CreatedAt"`).
- Soft-deleted aggregates are excluded from queries via a global filter
  (`IgnoreQueryFilters()` to opt out).
- `Domium.Benchmarks` (BenchmarkDotNet) for pipeline dispatch overhead.

### Fixed

- Redis tag invalidation actually works (metadata was never persisted before); value + tag
  writes are transactional and tag sets carry TTLs.
- Handler exceptions keep their original stack traces; event dispatch uses cached compiled
  invokers instead of per-publish reflection.
- Rollback and idempotency-release run on a non-cancellable token, so cancellation cannot
  leak transactions or reservations.
- `ExternalEventsPublished` counts only successful publishes; queries are fully instrumented.

## [0.1.0]

Initial preview.
