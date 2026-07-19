using Domium.Domain;
using Domium.Domain.Abstractions.Entity;
using Domium.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domium.Tests.Persistence;

public sealed class ShadowPropertyConfigurationTests
{
    [Fact]
    public void Soft_deletable_only_aggregate_gets_is_deleted()
    {
        using var context = new ShadowContext();

        var entityType = context.Model.FindEntityType(typeof(SoftOnly))!;

        Assert.NotNull(entityType.FindProperty(DomiumShadowPropertyNames.IsDeleted));
    }

    [Fact]
    public void Soft_deletable_only_aggregate_does_not_get_deletion_audit_columns()
    {
        using var context = new ShadowContext();

        var entityType = context.Model.FindEntityType(typeof(SoftOnly))!;

        Assert.Null(entityType.FindProperty(DomiumShadowPropertyNames.DeletedAt));
        Assert.Null(entityType.FindProperty(DomiumShadowPropertyNames.DeletedBy));
        Assert.Null(entityType.FindProperty(DomiumShadowPropertyNames.CreatedAt));
        Assert.Null(entityType.FindProperty(DomiumShadowPropertyNames.ModifiedAt));
    }

    [Fact]
    public void Auditable_only_aggregate_gets_created_and_modified_but_no_deletion_columns()
    {
        using var context = new ShadowContext();

        var entityType = context.Model.FindEntityType(typeof(AuditOnly))!;

        Assert.NotNull(entityType.FindProperty(DomiumShadowPropertyNames.CreatedAt));
        Assert.NotNull(entityType.FindProperty(DomiumShadowPropertyNames.ModifiedAt));
        Assert.Null(entityType.FindProperty(DomiumShadowPropertyNames.IsDeleted));
        Assert.Null(entityType.FindProperty(DomiumShadowPropertyNames.DeletedAt));
    }

    [Fact]
    public void Auditable_and_soft_deletable_aggregate_gets_deletion_audit_columns()
    {
        using var context = new ShadowContext();

        var entityType = context.Model.FindEntityType(typeof(AuditedSoft))!;

        Assert.NotNull(entityType.FindProperty(DomiumShadowPropertyNames.IsDeleted));
        Assert.NotNull(entityType.FindProperty(DomiumShadowPropertyNames.DeletedAt));
        Assert.NotNull(entityType.FindProperty(DomiumShadowPropertyNames.DeletedBy));
    }

    private sealed class ShadowId(Guid value) : AggregateId<Guid>(value);

    private sealed class SoftOnly : AggregateRoot<ShadowId>, ISoftDeletableEntity
    {
        private SoftOnly() : base(new ShadowId(Guid.NewGuid()))
        {
        }
    }

    private sealed class AuditOnly : AggregateRoot<ShadowId>, IAuditableEntity
    {
        private AuditOnly() : base(new ShadowId(Guid.NewGuid()))
        {
        }
    }

    private sealed class AuditedSoft : AggregateRoot<ShadowId>, IAuditableEntity, ISoftDeletableEntity
    {
        private AuditedSoft() : base(new ShadowId(Guid.NewGuid()))
        {
        }
    }

    private sealed class SoftOnlyConfiguration : BaseAggregateConfiguration<SoftOnly>
    {
        protected override void ConfigureAggregate(EntityTypeBuilder<SoftOnly> builder)
        {
        }
    }

    private sealed class AuditOnlyConfiguration : BaseAggregateConfiguration<AuditOnly>
    {
        protected override void ConfigureAggregate(EntityTypeBuilder<AuditOnly> builder)
        {
        }
    }

    private sealed class AuditedSoftConfiguration : BaseAggregateConfiguration<AuditedSoft>
    {
        protected override void ConfigureAggregate(EntityTypeBuilder<AuditedSoft> builder)
        {
        }
    }

    private sealed class ShadowContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options) =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("N"));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new SoftOnlyConfiguration());
            modelBuilder.ApplyConfiguration(new AuditOnlyConfiguration());
            modelBuilder.ApplyConfiguration(new AuditedSoftConfiguration());
        }
    }
}
