using Domium.Domain.Abstractions.Aggregate;
using Domium.Domain.Abstractions.Entity;
using Domium.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domium.Persistence.EntityFrameworkCore;

public abstract class BaseEntityConfiguration<TAggregate> : IEntityTypeConfiguration<TAggregate>, IEntityConfiguration<TAggregate>
    where TAggregate : class, IAggregateRoot
{
    public void Configure(EntityTypeBuilder<TAggregate> builder)
    {
        ConfigureTable(builder);
        ConfigureKey(builder);
        ConfigureDomainEvents(builder);
        ConfigureCrossCuttingShadowProperties(builder);
        ConfigureAggregate(builder);
    }

    protected abstract string TableName { get; }

    protected abstract string Schema { get; }

    protected abstract void ConfigureAggregate(EntityTypeBuilder<TAggregate> builder);

    protected virtual void ConfigureTable(EntityTypeBuilder<TAggregate> builder)
    {
        builder.ToTable(TableName, Schema);
    }

    protected virtual void ConfigureKey(EntityTypeBuilder<TAggregate> builder) =>
        builder.HasKey("Id");

    protected virtual void ConfigureDomainEvents(EntityTypeBuilder<TAggregate> builder) =>
        builder.Ignore(x => x.DomainEvents);

    protected virtual void ConfigureCrossCuttingShadowProperties(EntityTypeBuilder<TAggregate> builder)
    {
        if (typeof(IAuditableEntity).IsAssignableFrom(typeof(TAggregate)))
        {
            AuditableShadowProperties(builder);
        }

        if (typeof(ISoftDeletableEntity).IsAssignableFrom(typeof(TAggregate)))
        {
            SoftDeleteShadowProperties(builder);
        }
        if (typeof(ISoftDeletableEntity).IsAssignableFrom(typeof(TAggregate))&& typeof(IAuditableEntity).IsAssignableFrom(typeof(TAggregate)))
        {
            DeletedByShadowProperty(builder);
        }
    }

    protected static PropertyBuilder<string> RequiredString(PropertyBuilder<string> property, int maxLength) =>
        property.HasMaxLength(maxLength).IsRequired();

    protected static PropertyBuilder<string?> OptionalString(PropertyBuilder<string?> property, int maxLength) =>
        property.HasMaxLength(maxLength);

    protected static void AuditableShadowProperties(
        EntityTypeBuilder<TAggregate> builder,
        int actorMaxLength = 160)
    {
        builder.Property<DateTimeOffset>(DomiumShadowPropertyNames.CreatedAt);
        builder.Property<DateTimeOffset?>(DomiumShadowPropertyNames.ModifiedAt);
        builder.Property<string?>(DomiumShadowPropertyNames.CreatedBy).HasMaxLength(actorMaxLength);
        builder.Property<string?>(DomiumShadowPropertyNames.ModifiedBy).HasMaxLength(actorMaxLength);
    }

    protected static void SoftDeleteShadowProperties(
        EntityTypeBuilder<TAggregate> builder,
        int actorMaxLength = 160)
    {
        builder.Property<bool>(DomiumShadowPropertyNames.IsDeleted);
        builder.Property<DateTimeOffset?>(DomiumShadowPropertyNames.DeletedAt);
        
    }

    protected static PropertyBuilder<string?> DeletedByShadowProperty(
        EntityTypeBuilder<TAggregate> builder,
        int maxLength = 160) =>
        builder
            .Property<string?>(DomiumShadowPropertyNames.DeletedBy)
            .HasMaxLength(maxLength);
}
