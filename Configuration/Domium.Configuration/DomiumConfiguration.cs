using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Command.PipeLines;
using Domium.Application.Abstractions.Command.Validation;
using Domium.Application.Abstractions.Events;
using Domium.Application.Abstractions.Query;
using Domium.Application.Abstractions.Query.Pipelines;
using Domium.Application.Abstractions.Query.Validation;
using Domium.Application.Command;
using Domium.Application.Command.Pipelines.Behaviors;
using Domium.Application.Query;
using Domium.Application.Query.Pipelines.Behaviors;
using Domium.Caching.Abstractions;
using Domium.Caching.Memory;
using Domium.Eventing;
using Domium.Eventing.Abstractions.External;
using Domium.Eventing.Abstractions.Internal;
using Domium.Facade.Abstractions;
using Domium.Idempotency.Abstractions.Models;
using Domium.Idempotency.Abstractions.Providers;
using Domium.Idempotency.Providers;
using Domium.Persistence.Abstractions;
using Domium.Tenancy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scrutor;
using System.Reflection;

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

        ValidateNoDuplicateHandlers(assemblies, typeof(ICommandHandler<>), "command");
        ValidateNoDuplicateHandlers(assemblies, typeof(ICommandHandler<,>), "command");
        ValidateNoDuplicateHandlers(assemblies, typeof(IQueryHandler<,>), "query");

        services.Scan(scan => scan
            .FromAssemblies(assemblies)

            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsImplementedInterfaces()
            .WithScopedLifetime()

            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
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
            .WithScopedLifetime());

        RegisterApplicationServices(services, options, assemblies);
    }

    /// <summary>
    /// Registers "application services" (repositories, read models, domain-service
    /// implementations) by convention: a class is registered against an interface only when
    /// BOTH the class and the interface live in assemblies that were explicitly added via
    /// <see cref="DomiumOptions.AddApplicationAssembly"/>. This keeps modules isolated —
    /// an implementation can never be silently bound to another module's (or a third
    /// party's) interface.
    /// </summary>
    private static void RegisterApplicationServices(
        IServiceCollection services,
        DomiumOptions options,
        Assembly[] scannedAssemblies)
    {
        var applicationServiceAssemblies = GetApplicationServiceAssemblies(options, scannedAssemblies)
            .Where(assembly => assembly is { IsDynamic: false })
            .Distinct()
            .ToArray();

        if (applicationServiceAssemblies.Length == 0)
        {
            return;
        }

        var assemblySet = new HashSet<Assembly>(applicationServiceAssemblies);

        services.Scan(scan => scan
            .FromAssemblies(applicationServiceAssemblies)
            .AddClasses(c => c.Where(type => GetApplicationServiceInterfaces(type, assemblySet).Any()))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .As(type => GetApplicationServiceInterfaces(type, assemblySet))
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

    private static IEnumerable<Assembly> GetApplicationServiceAssemblies(
        DomiumOptions options,
        IEnumerable<Assembly> scannedAssemblies)
    {
        foreach (var assembly in options.ApplicationAssemblies)
        {
            yield return assembly;
        }

        if (options.ApplicationAssemblyNamePrefixes.Count == 0)
        {
            yield break;
        }

        foreach (var assembly in scannedAssemblies)
        {
            var name = assembly.GetName().Name;
            if (!string.IsNullOrWhiteSpace(name) &&
                options.ApplicationAssemblyNamePrefixes.Any(
                    prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            {
                yield return assembly;
            }
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
               !name.StartsWith("MassTransit", StringComparison.Ordinal) &&
               !name.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) &&
               !name.StartsWith("mscorlib", StringComparison.Ordinal) &&
               !name.StartsWith("netstandard", StringComparison.Ordinal) &&
               !IsDomiumFrameworkAssembly(name);
    }

    private static bool IsDomiumFrameworkAssembly(string name)
    {
        return name is "Domium"
            or "Domium.Configuration"
            or "Domium.Domain"
            or "Domium.Domain.Abstractions"
            or "Domium.Application"
            or "Domium.Application.Abstractions"
            or "Domium.Persistence.Abstractions"
            or "Domium.Persistence.EntityFrameworkCore"
            or "Domium.Persistence.Dapper"
            or "Domium.Caching.Abstractions"
            or "Domium.Caching.Memory"
            or "Domium.Caching.Redis"
            or "Domium.Eventing"
            or "Domium.Eventing.Abstractions"
            or "Domium.Eventing.MassTransit"
            or "Domium.Facade"
            or "Domium.Facade.Abstractions"
            or "Domium.Idempotency"
            or "Domium.Idempotency.Abstractions"
            or "Domium.Observability"
            or "Domium.Observability.OpenTelemetry"
            or "Domium.Querying"
            or "Domium.Querying.Abstractions"
            or "Domium.Querying.EntityFrameworkCore"
            or "Domium.Tenancy"
            or "Domium.Tenancy.Abstractions"
            or "Domium.Extensions.DependencyInjection";
    }

    private static IEnumerable<Type> GetApplicationServiceInterfaces(Type type, HashSet<Assembly> assemblySet)
    {
        return type
            .GetInterfaces()
            .Where(serviceType => IsApplicationServiceInterface(serviceType, assemblySet))
            .Distinct();
    }

    private static bool IsApplicationServiceInterface(Type serviceType, HashSet<Assembly> assemblySet)
    {
        if (serviceType.IsGenericTypeDefinition ||
            serviceType.ContainsGenericParameters)
        {
            return false;
        }

        // Only interfaces the application explicitly brought into the scan may be
        // auto-registered; framework/system/third-party interfaces never match.
        return assemblySet.Contains(serviceType.Assembly);
    }

    private static void RegisterOptionalBehaviors(IServiceCollection services, DomiumOptions options)
    {
        if (options.ObservabilityEnabled)
        {
            // Registered first so the activity span wraps every other behavior and the handler.
            AddCommandBehavior(services, typeof(ObservabilityCommandBehavior<>), typeof(ObservabilityCommandBehavior<,>));
            services.TryAddEnumerable(
                ServiceDescriptor.Scoped(typeof(IQueryPipelineBehavior<,>), typeof(ObservabilityQueryBehavior<,>)));
        }

        if (options.ValidationEnabled)
        {
            AddCommandBehavior(services, typeof(ValidationCommandBehavior<>), typeof(ValidationCommandBehavior<,>));
            services.TryAddEnumerable(
                ServiceDescriptor.Scoped(typeof(IQueryPipelineBehavior<,>), typeof(ValidationQueryBehavior<,>)));
        }

        if (options.LoggingEnabled)
        {
            AddCommandBehavior(services, typeof(LoggingCommandBehavior<>), typeof(LoggingCommandBehavior<,>));
            services.TryAddEnumerable(
                ServiceDescriptor.Scoped(typeof(IQueryPipelineBehavior<,>), typeof(LoggingQueryBehavior<,>)));
        }

        if (options.IdempotencyEnabled)
        {
            ValidateIdempotencyOptions(options.IdempotencyOptions);
            EnsureDomiumCache(services, options.IdempotencyOptions.Store);
            RegisterIdempotency(services, options.IdempotencyOptions);
        }

        if (options.TransactionsEnabled)
        {
            ValidateTransactionRegistration(services);
            AddCommandBehavior(services, typeof(TransactionCommandBehavior<>), typeof(TransactionCommandBehavior<,>));
        }

        if (options.CachingEnabled)
        {
            ValidateCachingOptions(options.CachingOptions);
            EnsureDomiumCache(services, options.CachingOptions.Store);

            services.TryAddSingleton(new DomiumQueryCachingOptions
            {
                DefaultDuration = options.CachingOptions.DefaultExpiration
            });

            services.TryAddEnumerable(
                ServiceDescriptor.Scoped(typeof(IQueryPipelineBehavior<,>), typeof(CachingQueryBehavior<,>)));
        }
    }

    private static void AddCommandBehavior(IServiceCollection services, Type voidBehavior, Type resultBehavior)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(ICommandPipelineBehavior<>), voidBehavior));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(ICommandPipelineBehavior<,>), resultBehavior));
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

    /// <summary>
    /// Registers the <see cref="IDomiumCache"/> store described by <paramref name="options"/>.
    /// Providers contribute the factory via their Use* extensions (UseMemory/UseRedis);
    /// when nothing was configured the in-memory store backs the feature. The first
    /// registration wins, so query caching and idempotency share one store.
    /// </summary>
    private static void EnsureDomiumCache(IServiceCollection services, DomiumCacheStoreOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        // Backs the default store; harmless when a custom store factory is configured.
        services.AddMemoryCache();

        var storeFactory = options.StoreFactory
            ?? (provider => new MemoryDomiumCache(provider.GetRequiredService<IMemoryCache>()));

        services.TryAddSingleton<IDomiumCache>(storeFactory);
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
        ValidateNoDuplicateRegisteredHandlers(services, typeof(ICommandHandler<>), "command");
        ValidateNoDuplicateRegisteredHandlers(services, typeof(ICommandHandler<,>), "command");
        ValidateNoDuplicateRegisteredHandlers(services, typeof(IQueryHandler<,>), "query");
    }

    private static void ValidateNoDuplicateHandlers(
        IEnumerable<Assembly> assemblies,
        Type openHandlerType,
        string kind)
    {
        var duplicates = GetClosedServiceImplementations(assemblies, openHandlerType)
            .GroupBy(pair => pair.ServiceType)
            .Where(group => group.Count() > 1)
            .Select(group => FormatDuplicateHandler(group.Key, group.Select(pair => pair.ImplementationType)))
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                $"Domium found multiple {kind} handlers: " + string.Join("; ", duplicates));
        }
    }

    private static void ValidateNoDuplicateRegisteredHandlers(
        IServiceCollection services,
        Type openHandlerType,
        string kind)
    {
        var duplicates = services
            .Where(d => d.ServiceType.IsGenericType &&
                        d.ServiceType.GetGenericTypeDefinition() == openHandlerType)
            .GroupBy(d => d.ServiceType)
            .Where(g => g.Count() > 1)
            .Select(g => FormatDuplicateRegistration(g.Key, g))
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                $"Domium found multiple {kind} handlers: " + string.Join(", ", duplicates));
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
}
