using System.Threading;
using Domium.Tenancy.Abstractions;

namespace Domium.Tenancy;

/// <summary>
/// Stores the current tenant context in the async execution flow.
/// </summary>
public sealed class AsyncLocalDomiumTenantContextAccessor : IDomiumTenantContextAccessor
{
    private static readonly AsyncLocal<DomiumTenantContext?> Current = new AsyncLocal<DomiumTenantContext?>();

    public DomiumTenantContext? GetCurrent()
    {
        return Current.Value ?? DomiumTenantContext.Unavailable;
    }

    public void SetCurrent(DomiumTenantContext tenantContext)
    {
        if (tenantContext == null)
        {
            throw new ArgumentNullException(nameof(tenantContext));
        }

        Current.Value = tenantContext;
    }

    public void ClearCurrent()
    {
        Current.Value = null;
    }
}
