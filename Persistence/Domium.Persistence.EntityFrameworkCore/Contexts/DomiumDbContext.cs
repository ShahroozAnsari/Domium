using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// Base EF Core DbContext for Domium write models. Derived contexts declare which
/// assemblies contain their <c>IEntityConfiguration</c> classes by overriding
/// <see cref="GetConfigurationAssemblies"/> — the model is built only from what the
/// context explicitly owns, keeping bounded contexts isolated and the model independent
/// of assembly load order. Domain events are published immediately through the aggregate's
/// injected <c>IEventBus</c>.
/// </summary>
public abstract class DomiumDbContext : DbContext
{
    protected DomiumDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// The assemblies whose Domium entity configurations make up this context's model.
    /// Return the infrastructure assemblies of the bounded context(s) this context owns.
    /// The default is empty — a context that overrides neither this method nor
    /// <see cref="OnModelCreating"/> maps nothing.
    /// </summary>
    protected virtual IEnumerable<Assembly> GetConfigurationAssemblies()
    {
        yield break;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var assemblies = GetConfigurationAssemblies().ToArray();

        if (assemblies.Length > 0)
        {
            modelBuilder.ApplyDomiumEntityConfigurationsFromAssemblies(assemblies);
        }
    }
}
