using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Domium.Persistence.EntityFrameworkCore;

public abstract class DomiumDbContext(DbContextOptions options) : DbContext(options)
{
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
