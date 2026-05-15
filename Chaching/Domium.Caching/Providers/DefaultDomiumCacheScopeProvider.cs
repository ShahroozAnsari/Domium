using System;
using Domium.Caching.Abstractions.Models;
using Domium.Caching.Abstractions.Providers;
using Domium.Caching.Exceptions;
using Domium.Tenancy.Abstractions;


namespace Domium.Caching.Providers
{
    /// <summary>
    /// Resolves cache scopes based on the configured cache policy and current tenant context.
    /// </summary>
    public sealed class DefaultDomiumCacheScopeProvider : IDomiumCacheScopeProvider
    {
        private readonly IDomiumTenantAccessor _tenantAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDomiumCacheScopeProvider"/> class.
        /// </summary>
        /// <param name="tenantAccessor">
        /// The tenant accessor used to resolve the current tenant context.
        /// </param>
        public DefaultDomiumCacheScopeProvider(IDomiumTenantAccessor tenantAccessor)
        {
            _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
        }

        /// <summary>
        /// Resolves the runtime cache scope for the specified policy.
        /// </summary>
        /// <param name="policy">
        /// The query cache policy.
        /// </param>
        /// <returns>
        /// A resolved <see cref="DomiumCacheScope"/>.
        /// </returns>
        public DomiumCacheScope GetScope(DomiumQueryCachePolicy policy)
        {
            if (policy == null)
            {
                throw new ArgumentNullException(nameof(policy));
            }

            if (policy.ScopeMode == DomiumQueryCacheScopeMode.Global)
            {
                return DomiumCacheScope.Global();
            }

            var tenantContext = _tenantAccessor.GetCurrent();

            if (tenantContext == null || !tenantContext.IsAvailable || string.IsNullOrWhiteSpace(tenantContext.TenantId))
            {
                throw new DomiumCacheScopeResolutionException(
                    $"A tenant-scoped cache policy was requested for query type '{policy.QueryType.FullName}', but no tenant context is available.");
            }

            return DomiumCacheScope.Tenant(tenantContext.TenantId);
        }
    }
}
