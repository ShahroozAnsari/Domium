using Domium.Caching.Abstractions.Models;

namespace Domium.Caching.Abstractions.Providers;

/// <summary>
/// Creates cache entry options from query cache policies.
/// </summary>
public interface IDomiumCacheEntryOptionsFactory
{
    /// <summary>
    /// Creates cache entry options for the specified policy.
    /// </summary>
    /// <param name="policy">
    /// The query cache policy.
    /// </param>
    /// <returns>
    /// A <see cref="DomiumCacheEntryOptions"/> instance.
    /// </returns>
    DomiumCacheEntryOptions Create(DomiumQueryCachePolicy policy);
}