using System.Reflection;
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
using Domium.Caching.Redis.Stores;
using Domium.Caching.Stores;
using Domium.Domain.Abstractions.Events;
using Domium.Eventing;
using Domium.Eventing.Abstractions.External;
using Domium.Eventing.Abstractions.Internal;
using Domium.Facade.Abstractions;
using Domium.Idempotency;
using Domium.Idempotency.Abstractions.Models;
using Domium.Idempotency.Abstractions.Providers;
using Domium.Idempotency.Providers;
using Domium.Persistence.Abstractions;
using Domium.Tenancy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scrutor;
using StackExchange.Redis;

namespace Domium.Configuration;

public static class DomiumConfiguration
{
    public static IServiceCollection Register(
        IServiceCollection services,
        DomiumOptions options)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (options == null) throw new ArgumentNullException(nameof(options));

        RegisterCore(services);
        RegisterApplicationTypes(services, options);
        RegisterOptionalBehaviors(services, options);
        ValidateHandlers(services);

        return services;
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
        var assemblies = GetApplicationAssemblies(options)
            .Where(assembly => assembly is { IsDynamic: false })
            .Distinct()
            .ToArray();

        if (assemblies.Length == 0)
        {
            return;
        }

        ValidateNoDuplicateCommandHandlers(assemblies);
        ValidateNoDuplicateQueryHandlers(assemblies);

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
            .WithSingletonLifetime()

            .AddClasses(c => c.Where(HasApplicationServiceInterfaces))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .As(GetApplicationServiceInterfaces)
            .WithScopedLifetime());
    }

    private static IEnumerable<Assembly> GetApplicationAssemblies(DomiumOptions options)
    {
        if (options.LoadedAssemblyScanningEnabled)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (IsApplicationAssembly(assembly, options))
                {
                    yield return assembly;
                }
            }
        }

        foreach (var assembly in options.ApplicationAssemblies)
        {
            yield return assembly;
        }
    }

    private static bool IsApplicationAssembly(Assembly assembly, DomiumOptions options)
    {
        if (assembly.IsDynamic)
        {
            return false;
        }

        var name = assembly.GetName().Name;

        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (options.ApplicationAssemblyNamePrefixes.Count > 0 &&
            !options.ApplicationAssemblyNamePrefixes.Any(
                prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return false;
        }

        return !name.StartsWith("System.", StringComparison.Ordinal) &&
               !name.StartsWith("Microsoft.", StringComparison.Ordinal) &&
               !name.StartsWith("mscorlib", StringComparison.Ordinal) &&
               !name.StartsWith("netstandard", StringComparison.Ordinal) &&
               !IsDomiumFrameworkAssembly(name);
    }

    private static bool IsDomiumFrameworkAssembly(string name)
    {
        return name == "Domium" ||
               name == "Domium.Configuration" ||
               name == "Domium.Domain" ||
               name == "Domium.Domain.Abstractions" ||
               name == "Domium.Application" ||
               name == "Domium.Application.Abstractions" ||
               name == "Domium.Persistence.Abstractions" ||
               name == "Domium.Persistence.EntityFrameworkCore" ||
               name == "Domium.Persistence.Dapper" ||
               name == "Domium.Caching" ||
               name == "Domium.Caching.Abstractions" ||
               name == "Domium.Caching.Memory" ||
               name == "Domium.Caching.Redis" ||
               name == "Domium.Eventing" ||
               name == "Domium.Eventing.Abstractions" ||
               name == "Domium.Eventing.MassTransit" ||
               name == "Domium.Facade" ||
               name == "Domium.Facade.Abstractions" ||
               name == "Domium.Idempotency" ||
               name == "Domium.Idempotency.Abstractions" ||
               name == "Domium.Observability" ||
               name == "Domium.Observability.OpenTelemetry" ||
               name == "Domium.Tenancy" ||
               name == "Domium.Tenancy.Abstractions" ||
               name == "Domium.Extensions.DependencyInjection";
    }

    private static bool HasApplicationServiceInterfaces(Type type) =>
        GetApplicationServiceInterfaces(type).Any();

    private static IEnumerable<Type> GetApplicationServiceInterfaces(Type type)
    {
        return type
            .GetInterfaces()
            .Where(IsApplicationServiceInterface)
            .Distinct();
    }

    private static bool IsApplicationServiceInterface(Type serviceType)
    {
        var namespaceName = serviceType.Namespace;

        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            return false;
        }

        return !namespaceName.StartsWith("System", StringComparison.Ordinal) &&
               !namespaceName.StartsWith("Microsoft", StringComparison.Ordinal) &&
               !namespaceName.StartsWith("Domium", StringComparison.Ordinal);
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

        if (options.IdempotencyEnabled)
        {
            RegisterIdempotencyCacheStore(services, options.IdempotencyOptions.Store);
            RegisterIdempotency(services, options.IdempotencyOptions);
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
            RegisterQueryCacheStore(services, options.CachingOptions.Store);
            RegisterCaching(services, options.CachingOptions);
        }
    }

    private static void RegisterIdempotency(IServiceCollection services, DomiumIdempotencyOptions options)
    {
        ValidateIdempotencyOptions(options);

        services.TryAddSingleton(
            new DomiumIdempotencyBehaviorOptions
            {
                Expiration = options.Expiration,
                KeyPrefix = options.KeyPrefix,
                RequireIdempotencyKey = options.RequireIdempotencyKey
            });

        services.TryAddSingleton<IDomiumIdempotencyKeyProvider, DefaultDomiumIdempotencyKeyProvider>();

        services.TryAddEnumerable(
            ServiceDescriptor.Scoped(
                typeof(ICommandPipelineBehavior<>),
                typeof(IdempotencyCommandBehavior<>)));
    }

    private static void RegisterCaching(IServiceCollection services, DomiumCachingOptions options)
    {
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

    private static void RegisterQueryCacheStore(IServiceCollection services, DomiumCacheStoreOptions options)
    {
        ValidateCacheStoreOptions(options, "Query caching");

        services.AddMemoryCache();
        services.TryAddSingleton<IDomiumCacheKeyFactory, DefaultDomiumCacheKeyFactory>();
        services.TryAddSingleton<IDomiumCacheKeyProvider, DefaultDomiumCacheKeyProvider>();
        services.TryAddSingleton<IDomiumQueryCacheStore>(
            provider => new DomiumQueryCacheStore(CreateCacheStore(provider, options)));
    }

    private static void RegisterIdempotencyCacheStore(IServiceCollection services, DomiumCacheStoreOptions options)
    {
        ValidateCacheStoreOptions(options, "Idempotency");

        services.AddMemoryCache();
        services.TryAddSingleton<IDomiumCacheKeyFactory, DefaultDomiumCacheKeyFactory>();
        services.TryAddSingleton<IDomiumIdempotencyCacheStore>(
            provider => new DomiumIdempotencyCacheStore(CreateCacheStore(provider, options)));
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

        ValidateCacheStoreOptions(options.Store, "Query caching");
    }

    private static void ValidateCacheStoreOptions(
        DomiumCacheStoreOptions options,
        string featureName)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        if (options.Provider == DomiumCacheProvider.Redis &&
            options.RedisConnectionFactory == null &&
            string.IsNullOrWhiteSpace(options.RedisConnectionString))
        {
            throw new InvalidOperationException(
                $"{featureName} Redis store requires a non-empty Redis connection string or Redis connection factory.");
        }
    }

    private static void ValidateIdempotencyOptions(DomiumIdempotencyOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        if (options.Expiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.Expiration),
                "Idempotency expiration must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.KeyPrefix))
        {
            throw new InvalidOperationException("Idempotency key prefix cannot be empty.");
        }

        ValidateCacheStoreOptions(options.Store, "Idempotency");
    }

    private static IDomiumCacheStore CreateCacheStore(
        IServiceProvider provider,
        DomiumCacheStoreOptions options)
    {
        if (options.Provider == DomiumCacheProvider.Memory)
        {
            return new MemoryDomiumCacheStore(provider.GetRequiredService<IMemoryCache>());
        }

        if (options.Provider == DomiumCacheProvider.Redis)
        {
            if (options.RedisConnectionFactory != null)
            {
                return new RedisDomiumCacheStore(options.RedisConnectionFactory(provider));
            }

            return new OwnedRedisDomiumCacheStore(
                ConnectionMultiplexer.Connect(options.RedisConnectionString));
        }

        throw new InvalidOperationException($"Unsupported cache provider '{options.Provider}'.");
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

    private static void ValidateHandlers(IServiceCollection services)
    {
        ValidateNoDuplicateRegisteredCommandHandlers(services);
        ValidateNoDuplicateRegisteredQueryHandlers(services);
    }

    private static void ValidateNoDuplicateCommandHandlers(IEnumerable<Assembly> assemblies)
    {
        var duplicates = GetClosedServiceImplementations(assemblies, typeof(ICommandHandler<>))
            .GroupBy(pair => pair.ServiceType)
            .Where(group => group.Count() > 1)
            .Select(group => FormatDuplicateHandler(group.Key, group.Select(pair => pair.ImplementationType)))
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                "Domium found multiple command handlers: " + string.Join("; ", duplicates));
        }
    }

    private static void ValidateNoDuplicateQueryHandlers(IEnumerable<Assembly> assemblies)
    {
        var duplicates = GetClosedServiceImplementations(assemblies, typeof(IQueryHandler<,>))
            .GroupBy(pair => pair.ServiceType)
            .Where(group => group.Count() > 1)
            .Select(group => FormatDuplicateHandler(group.Key, group.Select(pair => pair.ImplementationType)))
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                "Domium found multiple query handlers: " + string.Join("; ", duplicates));
        }
    }

    private static IEnumerable<(Type ServiceType, Type ImplementationType)> GetClosedServiceImplementations(
        IEnumerable<Assembly> assemblies,
        Type openGenericServiceType)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in GetLoadableTypes(assembly))
            {
                if (!type.IsClass || type.IsAbstract)
                {
                    continue;
                }

                foreach (var serviceType in type.GetInterfaces())
                {
                    if (serviceType.IsGenericType &&
                        serviceType.GetGenericTypeDefinition() == openGenericServiceType)
                    {
                        yield return (serviceType, type);
                    }
                }
            }
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null)!;
        }
    }

    private static string FormatDuplicateHandler(
        Type serviceType,
        IEnumerable<Type> implementationTypes)
    {
        var implementations = implementationTypes
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        return $"{serviceType.FullName ?? serviceType.Name} ({string.Join(", ", implementations)})";
    }

    private static void ValidateNoDuplicateRegisteredCommandHandlers(IServiceCollection services)
    {
        var duplicates = services
            .Where(d => d.ServiceType.IsGenericType &&
                        d.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
            .GroupBy(d => d.ServiceType)
            .Where(g => g.Count() > 1)
            .Select(g => FormatDuplicateRegistration(g.Key, g))
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                "Domium found multiple command handlers: " + string.Join(", ", duplicates));
        }
    }

    private static void ValidateNoDuplicateRegisteredQueryHandlers(IServiceCollection services)
    {
        var duplicates = services
            .Where(d => d.ServiceType.IsGenericType &&
                        d.ServiceType.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
            .GroupBy(d => d.ServiceType)
            .Where(g => g.Count() > 1)
            .Select(g => FormatDuplicateRegistration(g.Key, g))
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                "Domium found multiple query handlers: " + string.Join(", ", duplicates));
        }
    }

    private static string FormatDuplicateRegistration(
        Type serviceType,
        IEnumerable<ServiceDescriptor> descriptors)
    {
        var implementations = descriptors
            .Select(GetImplementationName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        return $"{serviceType.FullName ?? serviceType.Name} ({string.Join(", ", implementations)})";
    }

    private static string GetImplementationName(ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationType is not null)
        {
            return descriptor.ImplementationType.FullName ?? descriptor.ImplementationType.Name;
        }

        if (descriptor.ImplementationInstance is not null)
        {
            var instanceType = descriptor.ImplementationInstance.GetType();
            return instanceType.FullName ?? instanceType.Name;
        }

        return descriptor.ImplementationFactory is not null
            ? "<factory>"
            : "<unknown>";
    }

    private sealed class OwnedRedisDomiumCacheStore(IConnectionMultiplexer connectionMultiplexer)
        : IDomiumCacheStore, IDisposable
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        private readonly RedisDomiumCacheStore _inner = new(connectionMultiplexer);

        public Task<Domium.Caching.Abstractions.Results.DomiumCacheResult<T>> TryGetAsync<T>(
            string key,
            CancellationToken cancellationToken)
        {
            return _inner.TryGetAsync<T>(key, cancellationToken);
        }

        public Task SetAsync<T>(
            string key,
            T value,
            Domium.Caching.Abstractions.Models.DomiumCacheEntryOptions options,
            Domium.Caching.Abstractions.Models.DomiumCacheInvalidationMetadata invalidationMetadata,
            CancellationToken cancellationToken)
        {
            return _inner.SetAsync(key, value, options, invalidationMetadata, cancellationToken);
        }

        public Task<bool> TrySetAsync<T>(
            string key,
            T value,
            Domium.Caching.Abstractions.Models.DomiumCacheEntryOptions options,
            Domium.Caching.Abstractions.Models.DomiumCacheInvalidationMetadata invalidationMetadata,
            CancellationToken cancellationToken)
        {
            return _inner.TrySetAsync(key, value, options, invalidationMetadata, cancellationToken);
        }

        public Task RemoveAsync(
            string key,
            CancellationToken cancellationToken)
        {
            return _inner.RemoveAsync(key, cancellationToken);
        }

        public Task RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken)
        {
            return _inner.RemoveByTagAsync(tag, cancellationToken);
        }

        public Task RemoveByEntityKeyAsync(
            string entityKey,
            CancellationToken cancellationToken)
        {
            return _inner.RemoveByEntityKeyAsync(entityKey, cancellationToken);
        }

        public Task RemoveByGroupAsync(
            string group,
            CancellationToken cancellationToken)
        {
            return _inner.RemoveByGroupAsync(group, cancellationToken);
        }

        public void Dispose()
        {
            _connectionMultiplexer.Dispose();
        }
    }
}
