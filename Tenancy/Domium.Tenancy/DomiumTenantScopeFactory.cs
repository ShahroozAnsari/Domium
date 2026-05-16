using Domium.Tenancy.Abstractions;

namespace Domium.Tenancy;

/// <summary>
/// Creates scopes that temporarily set the ambient tenant context.
/// </summary>
public sealed class DomiumTenantScopeFactory(IDomiumTenantContextAccessor tenantContextAccessor)
    : IDomiumTenantScopeFactory
{
    private readonly IDomiumTenantContextAccessor _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));

    public IDisposable BeginScope(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant identifier cannot be null or empty.", nameof(tenantId));
        }

        var previous = _tenantContextAccessor.GetCurrent();
        _tenantContextAccessor.SetCurrent(new DomiumTenantContext(tenantId.Trim(), true));

        return new TenantScope(_tenantContextAccessor, previous);
    }

    private sealed class TenantScope(
        IDomiumTenantContextAccessor tenantContextAccessor,
        DomiumTenantContext? previous)
        : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (previous is null || !previous.IsAvailable)
            {
                tenantContextAccessor.ClearCurrent();
            }
            else
            {
                tenantContextAccessor.SetCurrent(previous);
            }

            _disposed = true;
        }
    }
}
