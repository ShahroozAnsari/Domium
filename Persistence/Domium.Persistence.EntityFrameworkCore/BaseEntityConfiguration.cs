using Domium.Domain.Abstractions.Aggregate;
using Domium.Domain.Abstractions.Entity;
using Domium.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Domium.Persistence.EntityFrameworkCore;

public abstract class BaseEntityConfiguration<TAggregate> : IEntityTypeConfiguration<TAggregate>, IEntityConfiguration<TAggregate>
    where TAggregate : class, IAggregateRoot
{
    private const int ActorMaxLength = 160;

    public void Configure(EntityTypeBuilder<TAggregate> builder)
    {
        ConfigureTable(builder);
        ConfigureKey(builder);
        ConfigureDomainEvents(builder);
        ConfigureAggregate(builder);
        ConfigureDomiumShadowProperties(builder.Metadata.Model.GetEntityTypes());
    }

    protected abstract string TableName { get; }

    protected abstract string Schema { get; }

    protected abstract void ConfigureAggregate(EntityTypeBuilder<TAggregate> builder);

    protected virtual void ConfigureTable(EntityTypeBuilder<TAggregate> builder)
    {
        builder.ToTable(TableName, Schema);
    }

    protected virtual void ConfigureKey(EntityTypeBuilder<TAggregate> builder)
    {
        builder.HasKey("Id");

        var idType = typeof(TAggregate).GetProperty("Id")?.PropertyType;
        if (idType is not null && typeof(IAggregateId<Guid>).IsAssignableFrom(idType))
        {
            var converter = (ValueConverter)Activator.CreateInstance(
                typeof(GuidAggregateIdConverter<>).MakeGenericType(idType))!;

            builder.Property(idType, "Id").HasConversion(converter);
        }
    }

    protected virtual void ConfigureDomainEvents(EntityTypeBuilder<TAggregate> builder) =>
        builder.Ignore(x => x.DomainEvents);

    protected static PropertyBuilder<string> RequiredString(PropertyBuilder<string> property, int maxLength) =>
        property.HasMaxLength(maxLength).IsRequired();

    protected static PropertyBuilder<string?> OptionalString(PropertyBuilder<string?> property, int maxLength) =>
        property.HasMaxLength(maxLength);

    private static void ConfigureDomiumShadowProperties(IEnumerable<IMutableEntityType> entityTypes)
    {
        foreach (var entityType in entityTypes)
        {
            var isAuditable = typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType);
            var isSoftDeletable = typeof(ISoftDeletableEntity).IsAssignableFrom(entityType.ClrType);

            if (isAuditable)
            {
                AddProperty(entityType, DomiumShadowPropertyNames.CreatedAt, typeof(DateTimeOffset));
                AddProperty(entityType, DomiumShadowPropertyNames.ModifiedAt, typeof(DateTimeOffset?), nullable: true);
                AddStringProperty(entityType, DomiumShadowPropertyNames.CreatedBy);
                AddStringProperty(entityType, DomiumShadowPropertyNames.ModifiedBy);
            }

            if (isSoftDeletable)
            {
                AddProperty(entityType, DomiumShadowPropertyNames.IsDeleted, typeof(bool));
                AddProperty(entityType, DomiumShadowPropertyNames.DeletedAt, typeof(DateTimeOffset?), nullable: true);
            }

            if (isAuditable && isSoftDeletable)
            {
                AddStringProperty(entityType, DomiumShadowPropertyNames.DeletedBy);
            }
        }
    }

    private static void AddStringProperty(IMutableEntityType entityType, string name)
    {
        var property = AddProperty(entityType, name, typeof(string), nullable: true);
        property.SetMaxLength(ActorMaxLength);
    }

    private static IMutableProperty AddProperty(
        IMutableEntityType entityType,
        string name,
        Type type,
        bool nullable = false)
    {
        var property = entityType.FindProperty(name) ?? entityType.AddProperty(name, type);

        if (nullable)
        {
            property.IsNullable = true;
        }

        return property;
    }

}

internal sealed class GuidAggregateIdConverter<TAggregateId>()
    : ValueConverter<TAggregateId, Guid>(
        id => id.Value,
        value => (TAggregateId)Activator.CreateInstance(typeof(TAggregateId), value)!)
    where TAggregateId : class, IAggregateId<Guid>;
