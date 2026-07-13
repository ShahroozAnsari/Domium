using System;

namespace Domium.Tenancy.Abstractions;

/// <summary>
/// The tenant currently in scope. Identified by <see cref="TenantId"/> — a stable key
/// (e.g. "acme") that drives the tenant database naming convention.
/// </summary>
public sealed class DomiumTenantContext
{
    /// <summary>A context representing the absence of a tenant.</summary>
    public static DomiumTenantContext Unavailable { get; } = new(null, false);

    private DomiumTenantContext(string? tenantId, bool isAvailable)
    {
        TenantId = tenantId;
        IsAvailable = isAvailable;
    }

    /// <summary>Creates an available tenant context for the given tenant id.</summary>
    public static DomiumTenantContext For(string tenantId) =>
        string.IsNullOrWhiteSpace(tenantId)
            ? throw new ArgumentException("Tenant id is required.", nameof(tenantId))
            : new DomiumTenantContext(tenantId.Trim(), true);

    /// <summary>The current tenant id, or <c>null</c> when unavailable.</summary>
    public string? TenantId { get; }

    /// <summary>Whether a tenant is in scope.</summary>
    public bool IsAvailable { get; }
}
