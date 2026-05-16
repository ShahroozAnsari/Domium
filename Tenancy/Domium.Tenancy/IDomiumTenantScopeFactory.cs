namespace Domium.Tenancy;

/// <summary>
/// Creates disposable tenant scopes.
/// </summary>
public interface IDomiumTenantScopeFactory
{
    /// <summary>
    /// Begins a scope for the supplied tenant identifier.
    /// </summary>
    IDisposable BeginScope(string tenantId);
}
