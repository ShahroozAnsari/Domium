using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Domium.Querying.Internal;

/// <summary>
/// Walks a dotted field path ("Category.Name") from a parameter expression, returning the
/// final member access and the leaf <see cref="PropertyInfo"/> where [Filterable]/[Sortable]
/// metadata lives. Every segment is validated; unknown segments throw a clear
/// <see cref="ArgumentException"/> that callers typically surface as a 400.
/// </summary>
internal static class PropertyPathResolver
{
    public static (MemberExpression Member, PropertyInfo LeafProperty) Resolve(
        ParameterExpression parameter,
        Type rootType,
        string field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            throw new ArgumentException("Field name cannot be empty.");
        }

        Expression current = parameter;
        var currentType = rootType;
        PropertyInfo? leaf = null;

        foreach (var segment in field.Split('.'))
        {
            leaf = currentType.GetProperty(
                      segment,
                      BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                ?? throw new ArgumentException(
                      $"Field '{field}' is invalid: no property '{segment}' on '{currentType.Name}'.");

            current = Expression.MakeMemberAccess(current, leaf);
            currentType = leaf.PropertyType;
        }

        return ((MemberExpression)current, leaf!);
    }
}
