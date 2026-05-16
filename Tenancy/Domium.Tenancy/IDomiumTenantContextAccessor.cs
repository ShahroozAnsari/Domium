using Domium.Tenancy.Abstractions;

namespace Domium.Tenancy;

/// <summary>
/// Provides read/write access to the ambient tenant context.
/// </summary>
public interface IDomiumTenantContextAccessor : IDomiumTenantAccessor
{
    /// <summary>
    /// Sets the current tenant context.
    /// </summary>
    void SetCurrent(DomiumTenantContext tenantContext);

    /// <summary>
    /// Clears the current tenant context.
    /// </summary>
    void ClearCurrent();
}
