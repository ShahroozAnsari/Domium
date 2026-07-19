using System.Linq.Expressions;
using Domium.Domain.Abstractions.Aggregate;
using Domium.Domain.Abstractions.Entity;
using Domium.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// Base for configurations of entities that manage their own table/key mapping
/// (e.g. read models); applies no Domium conventions.
/// </summary>
public abstract class BaseDerivedEntityConfiguration<TEntity>
     : IEntityTypeConfiguration<TEntity>, IEntityConfiguration<TEntity>
    where TEntity : class, IEntityBase
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        ConfigureAggregate(builder);
    }

    protected abstract void ConfigureAggregate(
        EntityTypeBuilder<TEntity> builder);
}

/// <summary>
/// Base configuration for aggregates: maps the table/schema, converts strongly-typed ids,
/// adds audit / soft-delete shadow properties for the configured entity, and applies the
/// soft-delete query filter so deleted aggregates never surface in reads.
/// </summary>
public abstract class BaseAggregateConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>, IEntityConfiguration<TEntity>
    where TEntity : class, IEntityBase
{
    private const int ActorMaxLength = 160;

    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        ConfigureTable(builder);
        ConfigureKey(builder);
        ConfigureAggregate(builder);
        ConfigureDomiumShadowProperties(builder);
    }

    protected virtual string TableName => typeof(TEntity).Name;

    protected virtual string Schema => GetSchemaName();

    protected abstract void ConfigureAggregate(EntityTypeBuilder<TEntity> builder);

    protected virtual void ConfigureTable(EntityTypeBuilder<TEntity> builder)
    {
        builder.ToTable(TableName, Schema);
    }

    protected virtual void ConfigureKey(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey("Id");

        // Ids are client-generated (Guid.CreateVersion7() in the ctor), never store-generated.
        // Without this, EF treats a non-default Guid key as an existing row and issues UPDATE
        // instead of INSERT for entities added to a tracked graph (e.g. child collections),
        // failing with "expected 1 row, affected 0".
        builder.Property("Id").ValueGeneratedNever();

        var idType = typeof(TEntity).GetProperty("Id")?.PropertyType;
        if (idType is not null && typeof(IAggregateId<Guid>).IsAssignableFrom(idType))
        {
            var converter = (ValueConverter)Activator.CreateInstance(
                typeof(GuidAggregateIdConverter<>).MakeGenericType(idType))!;

            builder.Property(idType, "Id").HasConversion(converter);
        }
    }

    protected static PropertyBuilder<string> RequiredString(PropertyBuilder<string> property, int maxLength) =>
        property.HasMaxLength(maxLength).IsRequired();

    protected static PropertyBuilder<string?> OptionalString(PropertyBuilder<string?> property, int maxLength) =>
        property.HasMaxLength(maxLength);

    private static void ConfigureDomiumShadowProperties(EntityTypeBuilder<TEntity> builder)
    {
        var isAuditable = typeof(IAuditableEntity).IsAssignableFrom(typeof(TEntity));
        var isSoftDeletable = typeof(ISoftDeletableEntity).IsAssignableFrom(typeof(TEntity));

        if (isAuditable)
        {
            builder.Property<DateTimeOffset>(DomiumShadowPropertyNames.CreatedAt);
            builder.Property<DateTimeOffset?>(DomiumShadowPropertyNames.ModifiedAt);
            builder.Property<string?>(DomiumShadowPropertyNames.CreatedBy).HasMaxLength(ActorMaxLength);
            builder.Property<string?>(DomiumShadowPropertyNames.ModifiedBy).HasMaxLength(ActorMaxLength);
        }

        if (isSoftDeletable)
        {
            builder.Property<bool>(DomiumShadowPropertyNames.IsDeleted).HasDefaultValue(false);

            // Soft-deleted aggregates are invisible to all queries unless the caller
            // explicitly opts out with IgnoreQueryFilters().
            builder.HasQueryFilter(entity => !EF.Property<bool>(entity, DomiumShadowPropertyNames.IsDeleted));
        }

        // Deletion timestamps and actors are auditing concerns: an aggregate that only opts
        // into soft delete records that it is deleted, not when or by whom.
        if (isAuditable && isSoftDeletable)
        {
            builder.Property<DateTimeOffset?>(DomiumShadowPropertyNames.DeletedAt);
            builder.Property<string?>(DomiumShadowPropertyNames.DeletedBy).HasMaxLength(ActorMaxLength);
        }

        if (typeof(IConcurrencyProtectedEntity).IsAssignableFrom(typeof(TEntity)))
        {
            // Optimistic concurrency: the interceptor bumps this on every update/delete, so
            // a writer holding a stale version fails with DomiumConcurrencyException.
            builder.Property<long>(DomiumShadowPropertyNames.Version)
                .IsConcurrencyToken()
                .HasDefaultValue(0L);
        }
    }

    private static string GetSchemaName()
    {
        var ns = typeof(TEntity).Namespace
                 ?? throw new InvalidOperationException(
                     $"Namespace for {typeof(TEntity).Name} is null.");

        var parts = ns.Split('.');

        if (parts.Length < 3)
        {
            throw new InvalidOperationException(
                $"Namespace '{ns}' does not contain at least three segments; override {nameof(Schema)} explicitly.");
        }

        return parts[2];
    }
}

/// <summary>
/// Converts strongly-typed Guid aggregate ids to and from the database Guid column.
/// The materialization side is a compiled constructor call — no per-row reflection.
/// </summary>
internal sealed class GuidAggregateIdConverter<TAggregateId>()
    : ValueConverter<TAggregateId, Guid>(id => id.Value, CreateFromGuidExpression())
    where TAggregateId : class, IAggregateId<Guid>
{
    private static Expression<Func<Guid, TAggregateId>> CreateFromGuidExpression()
    {
        var constructor = typeof(TAggregateId).GetConstructor(new[] { typeof(Guid) })
            ?? throw new InvalidOperationException(
                $"{typeof(TAggregateId).Name} must expose a public constructor taking a Guid to be usable as an aggregate id.");

        var value = Expression.Parameter(typeof(Guid), "value");
        return Expression.Lambda<Func<Guid, TAggregateId>>(Expression.New(constructor, value), value);
    }
}
