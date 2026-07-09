using System.Reflection;
using Domium.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Domium.Persistence.EntityFrameworkCore;

public static class ModelBuilderEntityConfigurationExtensions
{
    public static ModelBuilder ApplyDomiumEntityConfigurationsFromAssembly(this ModelBuilder modelBuilder, Assembly assembly)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(assembly, IsDomiumEntityConfiguration);
        return modelBuilder;
    }

    public static ModelBuilder ApplyDomiumEntityConfigurationsFromAssemblies(
        this ModelBuilder modelBuilder,
        IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies.Distinct())
        {
            modelBuilder.ApplyDomiumEntityConfigurationsFromAssembly(assembly);
        }

        return modelBuilder;
    }

    private static bool IsDomiumEntityConfiguration(Type type) =>
        type.GetInterfaces().Any(@interface =>
            @interface.IsGenericType &&
            @interface.GetGenericTypeDefinition() == typeof(IEntityConfiguration<>));
}
