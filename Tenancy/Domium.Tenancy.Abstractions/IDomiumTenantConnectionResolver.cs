namespace Domium.Tenancy.Abstractions;

/// <summary>
/// Builds the connection string for a tenant's database by applying the
/// <c>{tenant}_{service}</c> naming convention to a base connection template.
/// </summary>
public interface IDomiumTenantConnectionResolver
{
    /// <summary>
    /// Resolves the connection string for the tenant currently in scope. Throws when no
    /// tenant is available.
    /// </summary>
    string Resolve(string serviceName, string baseConnectionString);

    /// <summary>
    /// Resolves the connection string for an explicitly named tenant — used when the tenant
    /// is not (yet) the ambient one, e.g. provisioning a new tenant database.
    /// </summary>
    string ResolveFor(string tenantId, string serviceName, string baseConnectionString);
}
