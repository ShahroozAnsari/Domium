namespace Domium.Caching.Abstractions.Models;

/// <summary>
/// Represents the kind of cache scope.
/// </summary>
public enum DomiumCacheScopeKind
{
    /// <summary>
    /// Indicates that the cache entry is shared globally.
    /// </summary>
    Global = 0,

    /// <summary>
    /// Indicates that the cache entry is scoped to a specific tenant.
    /// </summary>
    Tenant = 1
}