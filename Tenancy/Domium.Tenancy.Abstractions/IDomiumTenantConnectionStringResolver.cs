namespace Domium.Tenancy.Abstractions;

public interface IDomiumTenantConnectionStringResolver
{
    string Resolve(string serviceName, string connectionStringTemplate, string? tenantName);
}
