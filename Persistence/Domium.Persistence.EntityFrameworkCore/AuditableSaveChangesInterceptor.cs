using Domium.Domain.Abstractions.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Domium.Persistence.EntityFrameworkCore;

public sealed class AuditableSaveChangesInterceptor(
    TimeProvider timeProvider,
    IDomiumCurrentUserAccessor currentUserAccessor) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        var timestamp = timeProvider.GetUtcNow();
        var userId = Normalize(currentUserAccessor.UserId);

        foreach (var entry in dbContext.ChangeTracker.Entries<IAuditableEntity>())
        {
            ApplyAudit(entry, timestamp, userId);
        }
    }

    private static void ApplyAudit(
        EntityEntry<IAuditableEntity> entry,
        DateTimeOffset timestamp,
        string? userId)
    {
        if (entry.State == EntityState.Added)
        {
            SetShadowProperty(entry, DomiumShadowPropertyNames.CreatedAt, timestamp);
            SetShadowProperty(entry, DomiumShadowPropertyNames.CreatedBy, userId);
        }
        else if (entry.State == EntityState.Modified)
        {
            SetShadowProperty(entry, DomiumShadowPropertyNames.ModifiedAt, timestamp);
            SetShadowProperty(entry, DomiumShadowPropertyNames.ModifiedBy, userId);
        }
    }

    private static void SetShadowProperty<TValue>(
        EntityEntry<IAuditableEntity> entry,
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
