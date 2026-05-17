using Domium.Caching.Abstractions.Models;

namespace Domium.Caching.Abstractions.Providers;

/// <summary>
/// Registers query cache policies at application startup.
/// </summary>
public interface IDomiumQueryCachePolicyRegistry
{
    /// <summary>
    /// Registers or replaces a cache policy for its query type.
    /// </summary>
    void Register(DomiumQueryCachePolicy policy);
}
