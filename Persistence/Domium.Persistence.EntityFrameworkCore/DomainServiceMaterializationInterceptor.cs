using Domium.Domain.Abstractions.DomainService;
using Domium.Eventing.Abstractions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Domium.Persistence.EntityFrameworkCore;

public sealed class DomainServiceMaterializationInterceptor(IServiceProvider serviceProvider) : IMaterializationInterceptor
{
    private static readonly ConcurrentDictionary<Type, InjectableProperty[]> InjectableProperties = new();

    private readonly Dictionary<Type, object?> _resolvedServices = new();

    public object InitializedInstance(
        MaterializationInterceptionData materializationData,
        object entity)
    {
        foreach (var property in InjectableProperties.GetOrAdd(entity.GetType(), BuildInjectableProperties))
        {
            var service = ResolveService(property.ServiceType);
            if (service is not null)
            {
                property.Set(entity, service);
            }
        }

        return entity;
    }

    private object? ResolveService(Type serviceType)
    {
        if (!_resolvedServices.TryGetValue(serviceType, out var service))
        {
            service = typeof(IEventBus).IsAssignableFrom(serviceType)
                ? serviceProvider.GetService(serviceType) ?? serviceProvider.GetService<IEventBus>()
                : typeof(IDomainService).IsAssignableFrom(serviceType)
                    ? serviceProvider.GetService(serviceType)
                        ?? serviceProvider.GetServices<IDomainService>().FirstOrDefault(serviceType.IsInstanceOfType)
                    : null;

            _resolvedServices[serviceType] = service;
        }

        return service;
    }

    private static InjectableProperty[] BuildInjectableProperties(Type entityType)
    {
        var properties = new List<InjectableProperty>();

        for (var type = entityType; type is not null && type != typeof(object); type = type.BaseType)
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (property.SetMethod is null || property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                if (property.PropertyType == typeof(IEventBus) ||
                    typeof(IEventBus).IsAssignableFrom(property.PropertyType) ||
                    typeof(IDomainService).IsAssignableFrom(property.PropertyType))
                {
                    properties.Add(new InjectableProperty(
                        property.PropertyType,
                        BuildSetter(property)));
                }
            }
        }

        return properties.ToArray();

        static Action<object, object> BuildSetter(PropertyInfo property)
        {
            var entity = Expression.Parameter(typeof(object), "entity");
            var service = Expression.Parameter(typeof(object), "service");
            var callSetter = Expression.Call(
                Expression.Convert(entity, property.DeclaringType!),
                property.SetMethod!,
                Expression.Convert(service, property.PropertyType));

            return Expression.Lambda<Action<object, object>>(callSetter, entity, service).Compile();
        }
    }

    private sealed record InjectableProperty(
        Type ServiceType,
        Action<object, object> Set);
}
