namespace Domium.Tenancy.Abstractions;

/// <summary>
/// Resolves the tenant name/key that should be used by tenant-aware infrastructure.
/// </summary>
public interface IDomiumTenantNameResolver
{
    string? ResolveTenantName();
}
