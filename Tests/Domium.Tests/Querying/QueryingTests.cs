using Domium.Querying;
using Domium.Querying.Abstractions;
using Domium.Querying.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Domium.Tests.Querying;

public sealed class QueryingTests
{
    private static readonly Guid AlphaCategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static IQueryable<Product> Products => new[]
    {
        new Product { Name = "Alpha Chair", Price = 50m, Status = ProductStatus.Active, CategoryId = AlphaCategoryId, CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z") },
        new Product { Name = "Beta Table", Price = 150m, Status = ProductStatus.Active, CategoryId = Guid.NewGuid(), CreatedAt = DateTimeOffset.Parse("2026-02-01T00:00:00Z") },
        new Product { Name = "Gamma chair", Price = 250m, Status = ProductStatus.Retired, CategoryId = Guid.NewGuid(), CreatedAt = DateTimeOffset.Parse("2026-03-01T00:00:00Z") },
    }.AsQueryable();

    [Theory]
    [InlineData("Price:Gt:100", 2)]
    [InlineData("Price:Between:100|200", 1)]
    [InlineData("Price:In:50|250", 2)]
    [InlineData("Name:Contains:CHAIR", 2)]
    [InlineData("Name:StartsWith:alpha", 1)]
    [InlineData("Name:Eq:Beta Table", 1)]
    [InlineData("Name:Ne:Beta Table", 2)]
    public void ApplyFilters_translates_operators(string filters, int expectedCount)
    {
        var options = new QueryOptions { Filters = filters };

        var result = Products.ApplyFilters(options.ParseFilters()).ToList();

        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public void ApplyFilters_parses_guid_enum_and_date_values()
    {
        Assert.Single(Products.ApplyFilters(Parse($"CategoryId:Eq:{AlphaCategoryId}")));
        Assert.Single(Products.ApplyFilters(Parse("Status:Eq:Retired")));
        Assert.Equal(2, Products.ApplyFilters(Parse("CreatedAt:Gte:2026-02-01T00:00:00Z")).Count());
    }

    [Fact]
    public void ApplyFilters_rejects_fields_without_filterable_attribute()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Products.ApplyFilters(Parse("Secret:Eq:x")).ToList());

        Assert.Contains("not filterable", exception.Message);
    }

    [Fact]
    public void ApplyFilters_rejects_operators_not_allowed_on_the_field()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Products.ApplyFilters(Parse("Name:Gt:zzz")).ToList());

        Assert.Contains("not allowed", exception.Message);
    }

    [Fact]
    public void ApplyFilters_reports_unconvertible_values_clearly()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Products.ApplyFilters(Parse("Price:Gt:expensive")).ToList());

        Assert.Contains("not a valid", exception.Message);
    }

    [Fact]
    public void ApplySort_supports_multiple_keys_with_direction()
    {
        var sorted = Products.ApplySort("-Status,Name").ToList();

        // Retired (enum value 1) first descending, then actives ordered by name.
        Assert.Equal("Gamma chair", sorted[0].Name);
        Assert.Equal("Alpha Chair", sorted[1].Name);
        Assert.Equal("Beta Table", sorted[2].Name);
    }

    [Fact]
    public void ApplySort_rejects_fields_without_sortable_attribute()
    {
        var exception = Assert.Throws<ArgumentException>(() => Products.ApplySort("Secret").ToList());

        Assert.Contains("not sortable", exception.Message);
    }

    [Fact]
    public void ApplySort_caps_the_number_of_sort_keys()
    {
        var tooManyKeys = string.Join(",", Enumerable.Repeat("Name", 9));

        var exception = Assert.Throws<ArgumentException>(() => Products.ApplySort(tooManyKeys).ToList());

        Assert.Contains("Too many sort keys", exception.Message);
    }

    [Fact]
    public void ParseFilters_caps_the_number_of_conditions()
    {
        var options = new QueryOptions
        {
            Filters = string.Join(",", Enumerable.Repeat("Price:Gt:1", QueryOptions.MaxFilterCount + 1)),
        };

        var exception = Assert.Throws<ArgumentException>(() => options.ParseFilters());

        Assert.Contains("Too many filter conditions", exception.Message);
    }

    [Fact]
    public async Task ApplyQueryOptionsAsync_clamps_page_size_and_reports_totals()
    {
        await using var context = CreateCatalog(itemCount: 500);
        var options = new QueryOptions { PageSize = 100_000, SortBy = "Price" };

        var page = await context.Products.AsNoTracking().ApplyQueryOptionsAsync(options);

        Assert.Equal(QueryableEfCoreExtensions.DefaultMaxPageSize, page.Items.Count);
        Assert.Equal(500, page.TotalCount);
        Assert.Equal(QueryableEfCoreExtensions.DefaultMaxPageSize, page.PageSize);
    }

    [Fact]
    public async Task ApplyQueryOptionsAsync_filters_sorts_and_pages_end_to_end()
    {
        await using var context = CreateCatalog(itemCount: 30);
        var options = new QueryOptions
        {
            Filters = "Price:Gte:10",
            SortBy = "-Price",
            Page = 2,
            PageSize = 5,
        };

        var page = await context.Products.AsNoTracking().ApplyQueryOptionsAsync(options);

        Assert.Equal(5, page.Items.Count);
        Assert.Equal(20, page.TotalCount);           // prices 10..29 match
        Assert.Equal(24m, page.Items[0].Price);      // second page of descending prices
        Assert.Equal(4, page.TotalPages);
    }

    private static IReadOnlyList<FilterCriteria> Parse(string filters) =>
        new QueryOptions { Filters = filters }.ParseFilters();

    private static CatalogContext CreateCatalog(int itemCount)
    {
        var options = new DbContextOptionsBuilder<CatalogContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var context = new CatalogContext(options);
        for (var i = 0; i < itemCount; i++)
        {
            context.Products.Add(new Product
            {
                Name = $"Product {i:D3}",
                Price = i,
                Status = ProductStatus.Active,
                CategoryId = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        context.SaveChanges();
        return context;
    }

    private sealed class CatalogContext(DbContextOptions<CatalogContext> options) : DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().HasKey(product => product.Id);
        }
    }

    private sealed class Product
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        [Filterable(FilterOperator.Eq, FilterOperator.Ne, FilterOperator.Contains, FilterOperator.StartsWith)]
        [Sortable]
        public string Name { get; init; } = string.Empty;

        [Filterable]
        [Sortable]
        public decimal Price { get; init; }

        [Filterable(FilterOperator.Eq)]
        [Sortable]
        public ProductStatus Status { get; init; }

        [Filterable(FilterOperator.Eq, FilterOperator.In)]
        public Guid CategoryId { get; init; }

        [Filterable]
        [Sortable]
        public DateTimeOffset CreatedAt { get; init; }

        public string Secret { get; init; } = "hidden";
    }

    private enum ProductStatus
    {
        Active,
        Retired,
    }
}
