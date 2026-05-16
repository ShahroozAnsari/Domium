namespace Domium.Caching.Abstractions.Models;

/// <summary>
/// Represents the cache scope mode configured for a query.
/// </summary>
public enum DomiumQueryCacheScopeMode
{
    /// <summary>
    /// Indicates that the query should be cached globally.
    /// </summary>
    Global = 0,

    /// <summary>
    /// Indicates that the query should be cached per tenant.
    /// </summary>
    Tenant = 1
}