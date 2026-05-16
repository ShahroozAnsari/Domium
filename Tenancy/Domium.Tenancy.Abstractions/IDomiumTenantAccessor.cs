

namespace Domium.Tenancy.Abstractions
{
    /// <summary>
    /// Provides access to the current tenant context.
    /// </summary>
    public interface IDomiumTenantAccessor
    {
        /// <summary>
        /// Gets the current tenant context.
        /// </summary>
        /// <returns>
        /// The current tenant context.
        /// </returns>
        DomiumTenantContext? GetCurrent();
    }
}
