using System;
using System.Text;

namespace Domium.Tenancy.Abstractions;

public static class DomiumTenantDatabaseName
{
    public static string Create(string serviceName, string? tenantName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("Service name is required.", nameof(serviceName));
        }

        var normalizedServiceName = Normalize(serviceName);
        if (string.IsNullOrWhiteSpace(tenantName))
        {
            return normalizedServiceName;
        }

        return $"{Normalize(tenantName)}_{normalizedServiceName}";
    }

    public static string ApplyToTemplate(string template, string serviceName, string? tenantName)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new ArgumentException("Connection string template is required.", nameof(template));
        }

        var normalizedServiceName = Normalize(serviceName);
        var normalizedTenantName = string.IsNullOrWhiteSpace(tenantName) ? string.Empty : Normalize(tenantName);
        var databaseName = Create(normalizedServiceName, normalizedTenantName);

        return template
            .Replace("{TenantDatabaseName}", databaseName, StringComparison.OrdinalIgnoreCase)
            .Replace("{DatabaseName}", databaseName, StringComparison.OrdinalIgnoreCase)
            .Replace("{TenantName}", normalizedTenantName, StringComparison.OrdinalIgnoreCase)
            .Replace("{ServiceName}", normalizedServiceName, StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", nameof(value));
        }

        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('_');
                previousWasSeparator = true;
            }
        }

        while (builder.Length > 0 && builder[builder.Length - 1] == '_')
        {
            builder.Length--;
        }

        if (builder.Length == 0)
        {
            throw new ArgumentException("Value must contain at least one letter or digit.", nameof(value));
        }

        return builder.ToString();
    }
}
