using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Command.PipeLines;
using Domium.Application.Abstractions.Command.Validation;
using Domium.Application.Abstractions.Query;
using Domium.Application.Abstractions.Query.Pipelines;
using Domium.Application.Abstractions.Query.Validation;
using Domium.Application.Command;
using Domium.Application.Command.Pipelines.Behaviors;
using Domium.Application.Events;
using Domium.Application.Query;
using Domium.Application.Query.Pipelines.Behaviors;
using Domium.Caching.Abstractions.Providers;
using Domium.Caching.Abstractions.Stores;
using Domium.Caching.Memory.Stores;
using Domium.Caching.Providers;
using Domium.Configuration;
using Domium.Domain.Abstractions.Events;
using Domium.Eventing;
using Domium.Eventing.Abstractions.External;
using Domium.Eventing.Abstractions.Internal;
using Domium.Facade.Abstractions;
using Domium.Persistence.Abstractions;
using Domium.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scrutor;

namespace Domium.Extensions.DependencyInjection;

internal static class DomiumRegistrar
{
    public static void Register(IDomiumBuilder builder)
    {
        RegisterCore(builder.Services);
        RegisterApplicationTypes(builder.Services, builder.Options);
        RegisterOptionalBehaviors(builder.Services, builder.Options);
        ValidateHandlers(builder.Services);
    }

    private static void RegisterCore(IServiceCollection services)
    {
        services.TryAddScoped<ICommandBus, CommandBus>();
        services.TryAddScoped<IQueryBus, QueryBus>();
        services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddDomiumEventing();
        services.AddDomiumTenancy();
    }

    private static void RegisterApplicationTypes(IServiceCollection services, DomiumOptions options)
    {
        var assemblies = options.ApplicationAssemblies
            .Where(assembly => assembly is { IsDynamic: false })
            .Distinct()
            .ToArray();

        if (assemblies.Length == 0)
        {
            return;
        }

        services.Scan(scan => scan
            .FromAssemblies(assemblies)

            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsImplementedInterfaces()
            .WithScopedLifetime()

            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsImplementedInterfaces()
            .WithScopedLifetime()

            .AddClasses(c => c.AssignableTo(typeof(ICommandValidator<>)))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsImplementedInterfaces()
            .WithScopedLifetime()

            .AddClasses(c => c.AssignableTo(typeof(IQueryValidator<,>)))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsImplementedInterfaces()
            .WithScopedLifetime()

            .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsImplementedInterfaces()
            .WithScopedLifetime()

            .AddClasses(c => c.AssignableTo(typeof(IInternalEventHandler<>)))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsImplementedInterfaces()
            .WithScopedLifetime()

            .AddClasses(c => c.AssignableTo(typeof(IExternalEventHandler<>)))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsImplementedInterfaces()
            .WithScopedLifetime()

            .AddClasses(c => c.AssignableTo<IFacade>())
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsImplementedInterfaces()
            .WithScopedLifetime()

            .AddClasses(c => c.AssignableTo(typeof(IDomiumQueryCachePolicyProvider)))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsImplementedInterfaces()
            .WithSingletonLifetime());
    }

    private static void RegisterOptionalBehaviors(IServiceCollection services, DomiumOptions options)
    {
        if (options.ValidationEnabled)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Scoped(
                    typeof(ICommandPipelineBehavior<>),
                    typeof(ValidationCommandBehavior<>)));

            services.TryAddEnumerable(
                ServiceDescriptor.Scoped(
                    typeof(IQueryPipelineBehavior<,>),
                    typeof(ValidationQueryBehavior<,>)));
        }

        if (options.LoggingEnabled)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Scoped(
                    typeof(ICommandPipelineBehavior<>),
                    typeof(LoggingCommandBehavior<>)));

            services.TryAddEnumerable(
                ServiceDescriptor.Scoped(
                    typeof(IQueryPipelineBehavior<,>),
                    typeof(LoggingQueryBehavior<,>)));
        }

        if (options.TransactionsEnabled)
        {
            ValidateTransactionRegistration(services);

            services.TryAddEnumerable(
                ServiceDescriptor.Scoped(
                    typeof(ICommandPipelineBehavior<>),
                    typeof(TransactionCommandBehavior<>)));
        }

        if (options.CachingEnabled)
        {
            RegisterCaching(services, options.CachingOptions);
        }
    }

    private static void RegisterCaching(IServiceCollection services, DomiumCachingOptions options)
    {
        ValidateCachingOptions(options);

        services.TryAddSingleton(options);

        if (options.Provider == DomiumCacheProvider.Memory)
        {
            services.AddMemoryCache();

            services.TryAddSingleton<IDomiumCacheStore, MemoryDomiumCacheStore>();
        }
        ValidateCacheStoreRegistration(services, options);

        services.TryAddSingleton<IDomiumCacheKeyProvider, DefaultDomiumCacheKeyProvider>();
        services.TryAddSingleton<DomiumQueryCachePolicyProvider>();
        services.TryAddSingleton<IDomiumQueryCachePolicyProvider>(
            provider => provider.GetRequiredService<DomiumQueryCachePolicyProvider>());
        services.TryAddSingleton<IDomiumQueryCachePolicyRegistry>(
            provider => provider.GetRequiredService<DomiumQueryCachePolicyProvider>());
        services.TryAddScoped<IDomiumCacheScopeProvider, DefaultDomiumCacheScopeProvider>();
        services.TryAddSingleton<IDomiumCacheInvalidationMetadataProvider, DefaultDomiumCacheInvalidationMetadataProvider>();
        services.TryAddSingleton<IDomiumCacheEntryOptionsFactory>(
            _ => new DefaultDomiumCacheEntryOptionsFactory(options.DefaultExpiration));

        services.TryAddEnumerable(
            ServiceDescriptor.Scoped(
                typeof(IQueryPipelineBehavior<,>),
                typeof(CachingQueryBehavior<,>)));
    }

    private static void ValidateCachingOptions(DomiumCachingOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        if (options.DefaultExpiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.DefaultExpiration),
                "Default cache expiration must be greater than zero.");
        }

        if (options.Provider == DomiumCacheProvider.Redis &&
            string.IsNullOrWhiteSpace(options.RedisConnectionString))
        {
            throw new InvalidOperationException(
                "Redis caching requires a non-empty Redis connection string.");
        }
    }

    private static void ValidateTransactionRegistration(IServiceCollection services)
    {
        var hasUnitOfWork = services.Any(service => service.ServiceType == typeof(IUnitOfWork));

        if (!hasUnitOfWork)
        {
            throw new InvalidOperationException(
                "Transactions require an IUnitOfWork registration. Register a persistence provider before calling AddDomium with UseTransactions.");
        }
    }

    private static void ValidateCacheStoreRegistration(
        IServiceCollection services,
        DomiumCachingOptions options)
    {
        if (options.Provider != DomiumCacheProvider.Redis)
        {
            return;
        }

        var hasCacheStore = services.Any(service => service.ServiceType == typeof(IDomiumCacheStore));

        if (!hasCacheStore)
        {
            throw new InvalidOperationException(
                "Redis caching requires an IDomiumCacheStore registration. Call AddDomiumRedisCacheStore before AddDomium.");
        }
    }

    private static void ValidateHandlers(IServiceCollection services)
    {
        ValidateNoDuplicateCommandHandlers(services);
        ValidateNoDuplicateQueryHandlers(services);
    }

    private static void ValidateNoDuplicateCommandHandlers(IServiceCollection services)
    {
        var duplicates = services
            .Where(d => d.ServiceType.IsGenericType &&
                        d.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
            .GroupBy(d => d.ServiceType)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.FullName ?? g.Key.Name)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                "Domium found multiple command handlers: " + string.Join(", ", duplicates));
        }
    }

    private static void ValidateNoDuplicateQueryHandlers(IServiceCollection services)
    {
        var duplicates = services
            .Where(d => d.ServiceType.IsGenericType &&
                        d.ServiceType.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
            .GroupBy(d => d.ServiceType)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key.FullName ?? g.Key.Name)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                "Domium found multiple query handlers: " + string.Join(", ", duplicates));
        }
    }
}
