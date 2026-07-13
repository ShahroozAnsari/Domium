namespace Domium.Tenancy.Abstractions;

/// <summary>
/// Resolves the identifier of the tenant currently in scope. The tenant id drives the
/// database naming convention (<c>{tenant}_{service}</c>) and therefore which physical
/// database a tenant-aware <c>DbContext</c> connects to.
/// </summary>
public interface IDomiumTenantResolver
{
    /// <summary>Returns the current tenant id, or <c>null</c> when no tenant is in scope.</summary>
    string? ResolveTenantId();
}
