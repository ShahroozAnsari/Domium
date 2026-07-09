using Domium.Domain.Abstractions.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Domium.Persistence.EntityFrameworkCore;

public sealed class SoftDeleteSaveChangesInterceptor(
    TimeProvider timeProvider,
    IDomiumCurrentUserAccessor currentUserAccessor) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplySoftDelete(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        var timestamp = timeProvider.GetUtcNow();
        foreach (var entry in dbContext.ChangeTracker.Entries<ISoftDeletableEntity>())
        {
            ApplySoftDelete(entry, timestamp);
        }
    }

    private void ApplySoftDelete(
        EntityEntry<ISoftDeletableEntity> entry,
        DateTimeOffset timestamp)
    {
        if (entry.State != EntityState.Deleted)
        {
            return;
        }

        SetShadowProperty(entry, DomiumShadowPropertyNames.IsDeleted, true);
        SetShadowProperty(entry, DomiumShadowPropertyNames.DeletedAt, timestamp);
        SetDeletedByShadowProperty(entry);

        entry.State = EntityState.Modified;
    }

    private void SetDeletedByShadowProperty(EntityEntry<ISoftDeletableEntity> entry)
    {
        if (entry.Metadata.FindProperty(DomiumShadowPropertyNames.DeletedBy) is null)
        {
            return;
        }

        entry.Property(DomiumShadowPropertyNames.DeletedBy).CurrentValue =
            Normalize(currentUserAccessor.UserId);
    }

    private static void SetShadowProperty<TValue>(
        EntityEntry<ISoftDeletableEntity> entry,
        string propertyName,
        TValue value)
    {
        if (entry.Metadata.FindProperty(propertyName) is null)
        {
            return;
        }

        entry.Property(propertyName).CurrentValue = value;
    }

    private static string? Normalize(string? actor) =>
        string.IsNullOrWhiteSpace(actor) ? null : actor.Trim();
}
