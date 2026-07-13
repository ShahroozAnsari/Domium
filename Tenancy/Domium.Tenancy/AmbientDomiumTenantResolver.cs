using Domium.Tenancy.Abstractions;

namespace Domium.Tenancy;

/// <summary>
/// Resolves the tenant id from the ambient tenant context (set by the request pipeline or a
/// <see cref="IDomiumTenantScopeFactory"/> scope). Applications may replace this with a
/// request-specific resolver (e.g. one that reads a claim or header).
/// </summary>
public sealed class AmbientDomiumTenantResolver(IDomiumTenantAccessor tenantAccessor)
    : IDomiumTenantResolver
{
    public string? ResolveTenantId()
    {
        var tenant = tenantAccessor.GetCurrent();
        return tenant?.IsAvailable == true && !string.IsNullOrWhiteSpace(tenant.TenantId)
            ? tenant.TenantId
            : null;
    }
}
