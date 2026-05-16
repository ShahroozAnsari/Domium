namespace Domium.Tenancy.Abstractions
{
    /// <summary>
    /// Represents the current tenant context.
    /// </summary>
    public sealed class DomiumTenantContext
    {
        /// <summary>
        /// Gets a tenant context that represents the absence of tenant information.
        /// </summary>
        public static DomiumTenantContext Unavailable { get; } = new DomiumTenantContext(null, false);

        /// <summary>
        /// Initializes a new instance of the <see cref="DomiumTenantContext"/> class.
        /// </summary>
        /// <param name="tenantId">
        /// The tenant identifier.
        /// </param>
        /// <param name="isAvailable">
        /// Indicates whether a tenant context is available.
        /// </param>
        public DomiumTenantContext(string? tenantId, bool isAvailable)
        {
            TenantId = tenantId;
            IsAvailable = isAvailable;
        }

        /// <summary>
        /// Gets the tenant identifier.
        /// </summary>
        public string? TenantId { get; }

        /// <summary>
        /// Gets a value indicating whether a tenant context is available.
        /// </summary>
        public bool IsAvailable { get; }
    }
}
