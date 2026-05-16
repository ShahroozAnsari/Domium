namespace Domium.Tenancy.Abstractions
{
    /// <summary>
    /// Provides an unavailable tenant context when an application has not registered tenancy.
    /// </summary>
    public sealed class DefaultDomiumTenantAccessor : IDomiumTenantAccessor
    {
        /// <inheritdoc />
        public DomiumTenantContext GetCurrent()
        {
            return DomiumTenantContext.Unavailable;
        }
    }
}
