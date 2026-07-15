using Domium.Domain.Abstractions.DomainService;
using Domium.Eventing.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// Injects <see cref="IEventBus"/> / <see cref="IDomainService"/> into materialized aggregates.
/// Stateless on purpose: interceptor instances form part of the DbContextOptions cache key, so a
/// new instance per DbContext makes EF build a new internal service provider each time
/// (ManyServiceProvidersCreatedWarning after 20). Register it as a SINGLETON — scoped services are
/// resolved per call from the context's own application service provider, not a captured one.
/// </summary>
public sealed class DomainServiceMaterializationInterceptor : IMaterializationInterceptor
{
    private static readonly ConcurrentDictionary<Type, InjectableMember[]> InjectableMembers = new();

    public object InitializedInstance(
        MaterializationInterceptionData materializationData,
        object entity)
    {
        var serviceProvider = GetApplicationServices(materializationData.Context);

        if (serviceProvider is null)
        {
            return entity;
        }

        foreach (var member in InjectableMembers.GetOrAdd(entity.GetType(), BuildInjectableMembers))
        {
            var service = ResolveService(serviceProvider, member.ServiceType);

            if (service is not null)
            {
                member.Set(entity, service);
            }
        }

        return entity;
    }

    /// <summary>
    /// The scope that created this DbContext. Resolving from here (rather than a captured provider)
    /// keeps scoped services correct while letting the interceptor itself stay a singleton.
    /// </summary>
    private static IServiceProvider? GetApplicationServices(DbContext context) =>
        context.GetService<IDbContextOptions>()
               .FindExtension<CoreOptionsExtension>()
               ?.ApplicationServiceProvider;

    private static object? ResolveService(
        IServiceProvider serviceProvider,
        Type serviceType)
    {
        if (typeof(IEventBus).IsAssignableFrom(serviceType))
        {
            return serviceProvider.GetService(serviceType)
                ?? serviceProvider.GetService<IEventBus>();
        }

        if (typeof(IDomainService).IsAssignableFrom(serviceType))
        {
            return serviceProvider.GetService(serviceType)
                ?? serviceProvider
                    .GetServices<IDomainService>()
                    .FirstOrDefault(serviceType.IsInstanceOfType);
        }

        return null;
    }

    private static InjectableMember[] BuildInjectableMembers(Type entityType)
    {
        var members = new List<InjectableMember>();

        for (var type = entityType;
             type is not null && type != typeof(object);
             type = type.BaseType)
        {
            foreach (var field in type.GetFields(
                         BindingFlags.Instance |
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.DeclaredOnly))
            {
                if (field.IsInitOnly)
                    continue;

                if (!IsInjectable(field.FieldType))
                    continue;

                members.Add(new InjectableMember(
                    field.FieldType,
                    BuildFieldSetter(field)));
            }

            foreach (var property in type.GetProperties(
                         BindingFlags.Instance |
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.DeclaredOnly))
            {
                if (property.SetMethod is null)
                    continue;

                if (property.GetIndexParameters().Length != 0)
                    continue;

                if (!IsInjectable(property.PropertyType))
                    continue;

                members.Add(new InjectableMember(
                    property.PropertyType,
                    BuildPropertySetter(property)));
            }
        }

        return members.ToArray();
    }

    private static bool IsInjectable(Type type)
    {
        return typeof(IEventBus).IsAssignableFrom(type)
            || typeof(IDomainService).IsAssignableFrom(type);
    }

    private static Action<object, object> BuildPropertySetter(PropertyInfo property)
    {
        var entity = Expression.Parameter(typeof(object), "entity");
        var service = Expression.Parameter(typeof(object), "service");

        var setter = Expression.Call(
            Expression.Convert(entity, property.DeclaringType!),
            property.SetMethod!,
            Expression.Convert(service, property.PropertyType));

        return Expression
            .Lambda<Action<object, object>>(setter, entity, service)
            .Compile();
    }

    private static Action<object, object> BuildFieldSetter(FieldInfo field)
    {
        var entity = Expression.Parameter(typeof(object), "entity");
        var service = Expression.Parameter(typeof(object), "service");

        var assign = Expression.Assign(
            Expression.Field(
                Expression.Convert(entity, field.DeclaringType!),
                field),
            Expression.Convert(service, field.FieldType));

        return Expression
            .Lambda<Action<object, object>>(assign, entity, service)
            .Compile();
    }

    private sealed record InjectableMember(
        Type ServiceType,
        Action<object, object> Set);
}