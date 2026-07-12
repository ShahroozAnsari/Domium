using System;
using System.Linq;

namespace Domium.Querying.Abstractions;

/// <summary>
/// Marks a property as filterable through Domium dynamic querying and lists which
/// operators are allowed on it. Also read by Domium.Querying.Swashbuckle to document
/// available filters in OpenAPI.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FilterableAttribute : Attribute
{
    public FilterableAttribute(params FilterOperator[] allowedOperators)
    {
        AllowedOperators = allowedOperators is { Length: > 0 }
            ? allowedOperators
            : (FilterOperator[])Enum.GetValues(typeof(FilterOperator));
    }

    /// <summary>
    /// The operators permitted on this property. Defaults to all operators
    /// when the attribute is applied without arguments.
    /// </summary>
    public FilterOperator[] AllowedOperators { get; }
}
