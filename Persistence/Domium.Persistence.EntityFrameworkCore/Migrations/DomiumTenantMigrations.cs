using Microsoft.EntityFrameworkCore;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// Schema management for tenant-per-database deployments. When the context has EF
/// migrations they are applied (creating the database on first run and upgrading existing
/// tenant databases on later deploys); a context without migrations falls back to
/// EnsureCreated, which can only create — never evolve — a schema. Author migrations before
/// the first production tenant exists so every later schema change has an upgrade path.
/// </summary>
public static class DomiumTenantMigrations
{
    /// <summary>
    /// Applies pending migrations to <paramref name="dbContext"/>'s database, or creates the
    /// schema via EnsureCreated when the context has no migrations. Returns <c>true</c> when
    /// migrations were used.
    /// </summary>
    public static async Task<bool> MigrateOrCreateAsync(
        DbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));

        if (dbContext.Database.GetMigrations().Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        await dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        return false;
    }

    /// <summary>
    /// Runs <see cref="MigrateOrCreateAsync"/> for every tenant in <paramref name="tenantIds"/> —
    /// the deploy-time loop that upgrades all tenant databases. The context for each tenant is
    /// produced by <paramref name="contextFactory"/> (typically: resolve the tenant's connection
    /// with IDomiumTenantConnectionResolver.ResolveFor and build options for your provider).
    /// </summary>
    public static async Task MigrateTenantsAsync<TContext>(
        IEnumerable<string> tenantIds,
        Func<string, TContext> contextFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        if (tenantIds == null) throw new ArgumentNullException(nameof(tenantIds));
        if (contextFactory == null) throw new ArgumentNullException(nameof(contextFactory));

        foreach (var tenantId in tenantIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var context = contextFactory(tenantId);
            await MigrateOrCreateAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }
}
