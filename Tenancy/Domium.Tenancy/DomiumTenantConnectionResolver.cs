using Domium.Tenancy.Abstractions;

namespace Domium.Tenancy;

public sealed class DomiumTenantConnectionResolver(IDomiumTenantResolver tenantResolver)
    : IDomiumTenantConnectionResolver
{
    public string Resolve(string serviceName, string baseConnectionString)
    {
        var tenantId = tenantResolver.ResolveTenantId()
            ?? throw new InvalidOperationException(
                $"No tenant is in scope; cannot resolve the '{serviceName}' database connection.");

        return DomiumTenantDatabaseName.ApplyToTemplate(baseConnectionString, serviceName, tenantId);
    }

    public string ResolveFor(string tenantId, string serviceName, string baseConnectionString) =>
        DomiumTenantDatabaseName.ApplyToTemplate(baseConnectionString, serviceName, tenantId);
}
