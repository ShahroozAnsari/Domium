using Domium.Domain.Abstractions.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// Increments the shadow Version concurrency token of <see cref="IConcurrencyProtectedEntity"/>
/// aggregates on every update or delete, so a concurrent writer's stale version fails the save.
/// </summary>
public sealed class ConcurrencyVersionSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        BumpVersions(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        BumpVersions(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void BumpVersions(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        foreach (var entry in dbContext.ChangeTracker.Entries<IConcurrencyProtectedEntity>())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (entry.Metadata.FindProperty(DomiumShadowPropertyNames.Version) is null)
            {
                continue;
            }

            var version = entry.Property<long>(DomiumShadowPropertyNames.Version);
            version.CurrentValue = (version.OriginalValue) + 1;
        }
    }
}
