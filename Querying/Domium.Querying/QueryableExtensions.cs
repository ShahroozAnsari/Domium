using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Domium.Querying.Abstractions;
using Domium.Querying.Internal;

namespace Domium.Querying;

/// <summary>
/// Applies <see cref="FilterCriteria"/> and sort requests to any <see cref="IQueryable{T}"/>
/// using expression trees built from the target type's [Filterable]/[Sortable] metadata.
/// Contains no EF Core dependency; works against any LINQ provider, including in-memory
/// collections. See Domium.Querying.EntityFrameworkCore for the async, paged, EF-aware
/// convenience wrapper.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies every filter in <paramref name="filters"/> to <paramref name="query"/>.
    /// Throws <see cref="ArgumentException"/> if a field is not filterable, an operator is
    /// not allowed on that field, or a value cannot be converted to the field's type.
    /// </summary>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IEnumerable<FilterCriteria> filters)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (filters == null) throw new ArgumentNullException(nameof(filters));

        var parameter = Expression.Parameter(typeof(T), "x");

        foreach (var filter in filters)
        {
            var (member, leafProperty) = PropertyPathResolver.Resolve(parameter, typeof(T), filter.Field);

            var attribute = leafProperty.GetCustomAttribute<FilterableAttribute>()
                ?? throw new ArgumentException($"Field '{filter.Field}' is not filterable on {typeof(T).Name}.");

            if (!attribute.AllowedOperators.Contains(filter.Operator))
            {
                throw new ArgumentException(
                    $"Operator '{filter.Operator}' is not allowed on field '{filter.Field}'. " +
                    $"Allowed: {string.Join(", ", attribute.AllowedOperators)}");
            }

            var body = FilterExpressionBuilder.Build(member, filter);
            query = query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
        }

        return query;
    }

    /// <summary>
    /// Applies one or more comma-separated sort keys, each optionally prefixed with "-" for
    /// descending — for example "Name,-CreatedAt". Throws <see cref="ArgumentException"/> if
    /// a field is not marked [Sortable].
    /// </summary>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string? sortBy)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        return string.IsNullOrWhiteSpace(sortBy)
            ? query
            : SortExpressionBuilder.Apply(query, sortBy!);
    }

    /// <summary>
    /// Applies filtering and sorting from <see cref="QueryOptions"/> in one call. Paging is
    /// intentionally left out here; use Domium.Querying.EntityFrameworkCore's
    /// ApplyQueryOptionsAsync for the full paged, async result.
    /// </summary>
    public static IQueryable<T> ApplyQueryOptions<T>(this IQueryable<T> query, QueryOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        return query.ApplyFilters(options.ParseFilters()).ApplySort(options.SortBy);
    }
}
