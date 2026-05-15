using System;

namespace Domium.Caching.Abstractions.Models
{
    /// <summary>
    /// Represents scope information used when building cache keys.
    /// </summary>
    public sealed class DomiumCacheScope
    {
        private DomiumCacheScope(DomiumCacheScopeKind kind, string tenantId)
        {
            Kind = kind;
            TenantId = tenantId;
        }

        /// <summary>
        /// Gets the cache scope kind.
        /// </summary>
        public DomiumCacheScopeKind Kind { get; }

        /// <summary>
        /// Gets the tenant identifier when the scope is tenant-specific.
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// Creates a global cache scope.
        /// </summary>
        /// <returns>
        /// A global cache scope.
        /// </returns>
        public static DomiumCacheScope Global()
        {
            return new DomiumCacheScope(DomiumCacheScopeKind.Global, null);
        }

        /// <summary>
        /// Creates a tenant cache scope.
        /// </summary>
        /// <param name="tenantId">
        /// The tenant identifier.
        /// </param>
        /// <returns>
        /// A tenant cache scope.
        /// </returns>
        public static DomiumCacheScope Tenant(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException("Tenant identifier cannot be null or empty.", nameof(tenantId));
            }

            return new DomiumCacheScope(DomiumCacheScopeKind.Tenant, tenantId);
        }
    }
}