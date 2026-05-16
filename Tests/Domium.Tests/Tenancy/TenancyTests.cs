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
}
