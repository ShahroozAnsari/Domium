using Domium.Extensions.DependencyInjection;
using Domium.Tenancy;
using Domium.Tenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Tests.Tenancy;

public sealed class TenancyTests
{
    [Fact]
    public void Tenant_scope_sets_and_restores_ambient_tenant()
    {
        var services = new ServiceCollection();

        services.AddDomium();

        using var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IDomiumTenantAccessor>();
        var scopeFactory = provider.GetRequiredService<IDomiumTenantScopeFactory>();

        Assert.False(accessor.GetCurrent()?.IsAvailable);

        using (scopeFactory.BeginScope("tenant-a"))
        {
            var tenant = accessor.GetCurrent();

            Assert.True(tenant?.IsAvailable);
            Assert.Equal("tenant-a", tenant?.TenantId);
        }

        Assert.False(accessor.GetCurrent()?.IsAvailable);
    }

    [Fact]
    public void Tenant_scopes_restore_nested_context()
    {
        var services = new ServiceCollection();

        services.AddDomium();

        using var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IDomiumTenantAccessor>();
        var scopeFactory = provider.GetRequiredService<IDomiumTenantScopeFactory>();

        using (scopeFactory.BeginScope("outer"))
        {
            using (scopeFactory.BeginScope("inner"))
            {
                Assert.Equal("inner", accessor.GetCurrent()?.TenantId);
            }

            Assert.Equal("outer", accessor.GetCurrent()?.TenantId);
        }
    }

    [Fact]
    public async Task Tenant_name_resolver_returns_ambient_tenant_id()
    {
        var services = new ServiceCollection();

        services.AddDomium();

        using var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IDomiumTenantScopeFactory>();

        var resolver = provider.GetRequiredService<IDomiumTenantNameResolver>();

        using (scopeFactory.BeginScope("acme"))
        {
            Assert.Equal("acme", resolver.ResolveTenantName());
        }
    }

    [Theory]
    [InlineData("tracking", null, "tracking")]
    [InlineData("tracking", "", "tracking")]
    [InlineData("tracking", "Acme Logistics", "acme_logistics_tracking")]
    [InlineData("Tracking Service", "North-East", "north_east_tracking_service")]
    public void Tenant_store_name_uses_tenant_id_when_available(
        string serviceName,
        string? tenantId,
        string expected)
    {
        Assert.Equal(expected, DomiumTenantDatabaseName.Create(serviceName, tenantId));
    }

    [Fact]
    public void Tenant_store_name_applies_database_template()
    {
        var connectionString = DomiumTenantDatabaseName.ApplyToTemplate(
            "Host=localhost;Database={DatabaseName}",
            "tracking",
            "Acme");

        Assert.Equal("Host=localhost;Database=acme_tracking", connectionString);
    }

    [Fact]
    public void Tenant_connection_string_resolver_applies_database_template()
    {
        var services = new ServiceCollection();

        services.AddDomium();

        using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IDomiumTenantConnectionStringResolver>();

        var connectionString = resolver.Resolve(
            "tracking",
            "Host=localhost;Database={DatabaseName};Tenant={TenantName}",
            "Acme");

        Assert.Equal("Host=localhost;Database=acme_tracking;Tenant=acme", connectionString);
    }
}
