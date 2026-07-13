using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Domium.Querying.Abstractions;

namespace Domium.Querying.Internal;

/// <summary>
/// Applies one or more sort keys ("Name,-CreatedAt") to a queryable. The first key uses
/// OrderBy/OrderByDescending, subsequent keys ThenBy/ThenByDescending. Every key must be
/// marked [Sortable] on the target type.
/// </summary>
internal static class SortExpressionBuilder
{
    private const int MaxSortKeys = 8;

    private static readonly MethodInfo OrderBy = QueryableMethod(nameof(Queryable.OrderBy));
    private static readonly MethodInfo OrderByDescending = QueryableMethod(nameof(Queryable.OrderByDescending));
    private static readonly MethodInfo ThenBy = QueryableMethod(nameof(Queryable.ThenBy));
    private static readonly MethodInfo ThenByDescending = QueryableMethod(nameof(Queryable.ThenByDescending));

    public static IQueryable<T> Apply<T>(IQueryable<T> query, string sortBy)
    {
        var ordered = false;
        var applied = 0;

        foreach (var rawKey in sortBy.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (++applied > MaxSortKeys)
            {
                throw new ArgumentException($"Too many sort keys; at most {MaxSortKeys} are allowed per request.");
            }

            var key = rawKey.Trim();
            var descending = key.StartsWith("-", StringComparison.Ordinal);
            var field = descending ? key.Substring(1) : key;

            var parameter = Expression.Parameter(typeof(T), "x");
            var (member, leafProperty) = PropertyPathResolver.Resolve(parameter, typeof(T), field);

            if (leafProperty.GetCustomAttribute<SortableAttribute>() is null)
            {
                throw new ArgumentException($"Field '{field}' is not sortable on {typeof(T).Name}.");
            }

            var keySelector = Expression.Lambda(member, parameter);
            var method = (ordered ? (descending ? ThenByDescending : ThenBy)
                                  : (descending ? OrderByDescending : OrderBy))
                .MakeGenericMethod(typeof(T), member.Type);

            query = (IQueryable<T>)method.Invoke(null, new object[] { query, keySelector })!;
            ordered = true;
        }

        return query;
    }

    private static MethodInfo QueryableMethod(string name) =>
        typeof(Queryable).GetMethods()
            .First(m => m.Name == name && m.GetParameters().Length == 2);
}
