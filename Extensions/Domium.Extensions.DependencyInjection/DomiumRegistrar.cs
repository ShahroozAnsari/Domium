using Domium.Application.Abstraction.Command;
using Domium.Application.Abstraction.Command.PipeLines;
using Domium.Application.Abstraction.Command.Validation;
using Domium.Application.Abstraction.Query;
using Domium.Application.Abstraction.Query.Pipelines;
using Domium.Application.Abstraction.Query.Validation;
using Domium.Application.Command;
using Domium.Application.Command.Pipelines.Behaviors;
using Domium.Application.Query;
using Domium.Application.Query.Pipelines.Behaviors;
using Domium.Caching.Abstractions.Providers;
using Domium.Caching.Abstractions.Providers;
using Domium.Caching.Abstractions.Results;
using Domium.Caching.Abstractions.Stores;
using Domium.Caching.Memory.Stores;
using Domium.Caching.Providers;
using Domium.Caching.Redis.Stores;
using Domium.Extensions.DependencyInjection.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyModel;
using Scrutor;

namespace Domium.Extensions.DependencyInjection;

internal static class DomiumRegistrar
{
    public static void Register(IDomiumBuilder builder)
    {
        RegisterCore(builder.Services);
        RegisterApplicationTypes(builder.Services);
        RegisterOptionalBehaviors(builder.Services, builder.Options);
        ValidateHandlers(builder.Services);
    }

    private static void RegisterCore(IServiceCollection services)
    {
        services.TryAddScoped<ICommandBus, CommandBus>();
        services.TryAddScoped<IQueryBus, QueryBus>();
    }

    private static void RegisterApplicationTypes(IServiceCollection services)
    {
        var dependencyContext = DependencyContext.Default;

        if (dependencyContext != null)
        {
            services.Scan(scan => scan
                .FromDependencyContext(dependencyContext, AssemblyFilter.IsCandidateAssembly)

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

                .AddClasses(c => c.AssignableTo(typeof(IDomiumQueryCachePolicyProvider)))
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsImplementedInterfaces()
                .WithSingletonLifetime());
        }
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
        services.TryAddSingleton(options);

        if (options.Provider == DomiumCacheProvider.Memory)
        {
            services.AddMemoryCache();

            services.TryAddSingleton<IDomiumCacheStore, MemoryDomiumCacheStore>();
        }
        else if (options.Provider == DomiumCacheProvider.Redis)
        {
            services.TryAddSingleton<IDomiumCacheStore, RedisDomiumCacheStore>();
        }

        services.TryAddSingleton<IDomiumCacheKeyProvider, DefaultDomiumCacheKeyProvider>();

        services.TryAddSingleton<IDomiumQueryCachePolicyProvider, DomiumQueryCachePolicyProvider>();

        services.TryAddEnumerable(
            ServiceDescriptor.Scoped(
                typeof(IQueryPipelineBehavior<,>),
                typeof(CachingQueryBehavior<,>)));
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
