using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Domium.Querying.Abstractions;

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
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (filters == null)
        {
            throw new ArgumentNullException(nameof(filters));
        }

        var parameter = Expression.Parameter(typeof(T), "x");

        foreach (var f in filters)
        {
            var (member, leafProperty) = ResolvePath(parameter, typeof(T), f.Field);

            var attr = leafProperty.GetCustomAttribute<FilterableAttribute>()
                ?? throw new ArgumentException($"Field '{f.Field}' is not filterable on {typeof(T).Name}.");

            if (!attr.AllowedOperators.Contains(f.Operator))
            {
                throw new ArgumentException(
                    $"Operator '{f.Operator}' is not allowed on field '{f.Field}'. " +
                    $"Allowed: {string.Join(", ", attr.AllowedOperators)}");
            }

            var body = BuildPredicateBody(member, f.Operator, f.Value, f.Field);
            var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    /// <summary>
    /// Applies a sort request, formatted as an optional "-" prefix (descending) followed by
    /// a field name, for example "-CreatedAt". Throws <see cref="ArgumentException"/> if the
    /// field is not sortable.
    /// </summary>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string? sortBy)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return query;
        }

        var descending = sortBy!.StartsWith("-", StringComparison.Ordinal);
        var field = descending ? sortBy.Substring(1) : sortBy;

        var parameter = Expression.Parameter(typeof(T), "x");
        var (member, leafProperty) = ResolvePath(parameter, typeof(T), field);

        if (leafProperty.GetCustomAttribute<SortableAttribute>() is null)
        {
            throw new ArgumentException($"Field '{field}' is not sortable on {typeof(T).Name}.");
        }

        var lambda = Expression.Lambda(member, parameter);

        var methodName = descending ? "OrderByDescending" : "OrderBy";
        var method = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), member.Type);

        return (IQueryable<T>)method.Invoke(null, new object[] { query, lambda })!;
    }

    /// <summary>
    /// Applies filtering and sorting from <see cref="QueryOptions"/> in one call. Paging is
    /// intentionally left out here since Skip/Take have no async materialization concerns on
    /// their own; use Domium.Querying.EntityFrameworkCore's ApplyQueryOptionsAsync for the
    /// full paged, async result.
    /// </summary>
    public static IQueryable<T> ApplyQueryOptions<T>(this IQueryable<T> query, QueryOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return query.ApplyFilters(options.ParseFilters()).ApplySort(options.SortBy);
    }

    /// <summary>
    /// Walks a dotted field path ("Category.Name") from the parameter expression, returning
    /// the final member access and the leaf PropertyInfo, where [Filterable]/[Sortable] must
    /// live. Validates every segment exists.
    /// </summary>
    private static (MemberExpression Member, PropertyInfo LeafProperty) ResolvePath(
        ParameterExpression parameter, Type rootType, string field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            throw new ArgumentException("Field name cannot be empty.");
        }

        var segments = field.Split('.');
        Expression current = parameter;
        var currentType = rootType;
        PropertyInfo? leaf = null;

        foreach (var segment in segments)
        {
            leaf = currentType.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                ?? throw new ArgumentException($"Field '{field}' is invalid: no property '{segment}' on '{currentType.Name}'.");

            current = Expression.MakeMemberAccess(current, leaf);
            currentType = leaf.PropertyType;
        }

        return ((MemberExpression)current, leaf!);
    }

    private static Expression BuildPredicateBody(MemberExpression member, FilterOperator op, string rawValue, string fieldName)
    {
        var targetType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;

        object ConvertOne(string s)
        {
            try
            {
                return targetType == typeof(string)
                    ? s
                    : Convert.ChangeType(s, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                throw new ArgumentException($"Value '{s}' for field '{fieldName}' is not a valid {targetType.Name}.");
            }
        }

        switch (op)
        {
            case FilterOperator.In:
                {
                    var values = rawValue.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(ConvertOne).ToList();
                    if (values.Count == 0)
                    {
                        throw new ArgumentException($"'In' filter on '{fieldName}' needs at least one value (pipe-separated).");
                    }

                    var listType = typeof(List<>).MakeGenericType(member.Type);
                    var typedList = (IList)Activator.CreateInstance(listType)!;
                    foreach (var v in values)
                    {
                        typedList.Add(v);
                    }

                    var containsMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                        .MakeGenericMethod(member.Type);

                    return Expression.Call(containsMethod, Expression.Constant(typedList, listType), member);
                }

            case FilterOperator.Between:
                {
                    var parts = rawValue.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                    {
                        throw new ArgumentException($"'Between' filter on '{fieldName}' needs exactly two pipe-separated values, e.g. '10|100'.");
                    }

                    var low = Expression.Constant(ConvertOne(parts[0]), member.Type);
                    var high = Expression.Constant(ConvertOne(parts[1]), member.Type);
                    return Expression.AndAlso(
                        Expression.GreaterThanOrEqual(member, low),
                        Expression.LessThanOrEqual(member, high));
                }

            default:
                {
                    var constant = Expression.Constant(ConvertOne(rawValue), member.Type);
                    return op switch
                    {
                        FilterOperator.Eq => Expression.Equal(member, constant),
                        FilterOperator.Ne => Expression.NotEqual(member, constant),
                        FilterOperator.Gt => Expression.GreaterThan(member, constant),
                        FilterOperator.Gte => Expression.GreaterThanOrEqual(member, constant),
                        FilterOperator.Lt => Expression.LessThan(member, constant),
                        FilterOperator.Lte => Expression.LessThanOrEqual(member, constant),
                        // ToUpper() on both sides gives case-insensitive matching that translates
                        // cleanly on SQL Server / PostgreSQL / SQLite via EF Core. Drop the
                        // ToUpper() calls for case-sensitive matching, or rely on DB collation.
                        FilterOperator.Contains => Expression.Call(
                            Expression.Call(member, nameof(string.ToUpper), null),
                            nameof(string.Contains), null,
                            Expression.Call(constant, nameof(string.ToUpper), null)),
                        FilterOperator.StartsWith => Expression.Call(
                            Expression.Call(member, nameof(string.ToUpper), null),
                            nameof(string.StartsWith), null,
                            Expression.Call(constant, nameof(string.ToUpper), null)),
                        FilterOperator.EndsWith => Expression.Call(
                            Expression.Call(member, nameof(string.ToUpper), null),
                            nameof(string.EndsWith), null,
                            Expression.Call(constant, nameof(string.ToUpper), null)),
                        _ => throw new ArgumentOutOfRangeException(nameof(op))
                    };
                }
        }
    }
}
