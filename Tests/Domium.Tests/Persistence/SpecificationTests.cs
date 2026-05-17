using Domium.Persistence.EntityFrameworkCore.Specifications;

namespace Domium.Tests.Persistence;

public sealed class SpecificationTests
{
    [Fact]
    public void Specification_captures_query_shape()
    {
        var specification = new ActiveCustomersSpecification();

        Assert.NotNull(specification.Criteria);
        Assert.Single(specification.Includes);
        Assert.NotNull(specification.OrderBy);
        Assert.True(specification.IsPagingEnabled);
        Assert.Equal(10, specification.Skip);
        Assert.Equal(25, specification.Take);
    }

    private sealed class ActiveCustomersSpecification : Specification<Customer>
    {
        public ActiveCustomersSpecification()
            : base(customer => customer.IsActive)
        {
            AddInclude(customer => customer.Address);
            ApplyOrderBy(customer => customer.Name);
            ApplyPaging(10, 25);
        }
    }

    private sealed class Customer
    {
        public bool IsActive { get; init; }

        public string Name { get; init; } = string.Empty;

        public Address Address { get; init; } = new Address();
    }

    private sealed class Address
    {
    }
}
