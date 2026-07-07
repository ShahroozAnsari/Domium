using Domium.Tenancy.Abstractions;

namespace Domium.Tenancy;

public sealed class AmbientDomiumTenantNameResolver(IDomiumTenantAccessor tenantAccessor)
    : IDomiumTenantNameResolver
{
    public string? ResolveTenantName()
    {
        var tenant = tenantAccessor.GetCurrent();
        return tenant?.IsAvailable == true && !string.IsNullOrWhiteSpace(tenant.TenantId)
            ? tenant.TenantId
            : null;
    }
}
