using Domium.Tenancy.Abstractions;

namespace Domium.Tenancy;

/// <summary>
/// Creates scopes that temporarily set the ambient tenant context.
/// </summary>
public sealed class DomiumTenantScopeFactory : IDomiumTenantScopeFactory
{
    private readonly IDomiumTenantContextAccessor _tenantContextAccessor;

    public DomiumTenantScopeFactory(IDomiumTenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

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

    private sealed class TenantScope : IDisposable
    {
        private readonly IDomiumTenantContextAccessor _tenantContextAccessor;
        private readonly DomiumTenantContext? _previous;
        private bool _disposed;

        public TenantScope(
            IDomiumTenantContextAccessor tenantContextAccessor,
            DomiumTenantContext? previous)
        {
            _tenantContextAccessor = tenantContextAccessor;
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_previous is null || !_previous.IsAvailable)
            {
                _tenantContextAccessor.ClearCurrent();
            }
            else
            {
                _tenantContextAccessor.SetCurrent(_previous);
            }

            _disposed = true;
        }
    }
}
