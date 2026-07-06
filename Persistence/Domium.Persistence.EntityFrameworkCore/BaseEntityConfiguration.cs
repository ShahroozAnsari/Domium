using Domium.Domain.Abstractions.Aggregate;
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

    protected static PropertyBuilder<string> RequiredString(PropertyBuilder<string> property, int maxLength) =>
        property.HasMaxLength(maxLength).IsRequired();

    protected static PropertyBuilder<string?> OptionalString(PropertyBuilder<string?> property, int maxLength) =>
        property.HasMaxLength(maxLength);
}
