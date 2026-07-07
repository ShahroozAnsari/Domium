using Domium.Tenancy.Abstractions;

namespace Domium.Tenancy;

public sealed class DomiumTenantConnectionStringResolver : IDomiumTenantConnectionStringResolver
{
    public string Resolve(string serviceName, string connectionStringTemplate, string? tenantName)
    {
        return DomiumTenantDatabaseName.ApplyToTemplate(connectionStringTemplate, serviceName, tenantName);
    }
}
