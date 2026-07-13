using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Domium.Querying.Abstractions;

namespace Domium.Querying.Internal;

/// <summary>
/// Builds the predicate body for one <see cref="FilterCriteria"/> against a resolved member.
/// One small method per operator family keeps each translation easy to read and test.
/// </summary>
internal static class FilterExpressionBuilder
{
    private static readonly MethodInfo EnumerableContains = typeof(Enumerable)
        .GetMethods()
        .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);

    public static Expression Build(MemberExpression member, FilterCriteria filter)
    {
        return filter.Operator switch
        {
            FilterOperator.In => BuildIn(member, filter),
            FilterOperator.Between => BuildBetween(member, filter),
            FilterOperator.Contains => BuildStringCall(member, filter, nameof(string.Contains)),
            FilterOperator.StartsWith => BuildStringCall(member, filter, nameof(string.StartsWith)),
            FilterOperator.EndsWith => BuildStringCall(member, filter, nameof(string.EndsWith)),
            _ => BuildComparison(member, filter)
        };
    }

    private static Expression BuildComparison(MemberExpression member, FilterCriteria filter)
    {
        var constant = Constant(member, filter.Value, filter.Field);

        return filter.Operator switch
        {
            FilterOperator.Eq => Expression.Equal(member, constant),
            FilterOperator.Ne => Expression.NotEqual(member, constant),
            FilterOperator.Gt => Expression.GreaterThan(member, constant),
            FilterOperator.Gte => Expression.GreaterThanOrEqual(member, constant),
            FilterOperator.Lt => Expression.LessThan(member, constant),
            FilterOperator.Lte => Expression.LessThanOrEqual(member, constant),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), $"Unsupported operator '{filter.Operator}'.")
        };
    }

    private static Expression BuildIn(MemberExpression member, FilterCriteria filter)
    {
        var values = SplitValues(filter.Value);
        if (values.Count == 0)
        {
            throw new ArgumentException(
                $"'In' filter on '{filter.Field}' needs at least one value (pipe-separated).");
        }

        var listType = typeof(List<>).MakeGenericType(member.Type);
        var typedList = (IList)Activator.CreateInstance(listType)!;
        foreach (var value in values)
        {
            typedList.Add(FilterValueParser.Parse(value, member.Type, filter.Field));
        }

        return Expression.Call(
            EnumerableContains.MakeGenericMethod(member.Type),
            Expression.Constant(typedList, listType),
            member);
    }

    private static Expression BuildBetween(MemberExpression member, FilterCriteria filter)
    {
        var bounds = SplitValues(filter.Value);
        if (bounds.Count != 2)
        {
            throw new ArgumentException(
                $"'Between' filter on '{filter.Field}' needs exactly two pipe-separated values, e.g. '10|100'.");
        }

        return Expression.AndAlso(
            Expression.GreaterThanOrEqual(member, Constant(member, bounds[0], filter.Field)),
            Expression.LessThanOrEqual(member, Constant(member, bounds[1], filter.Field)));
    }

    /// <summary>
    /// ToUpper() on both sides gives case-insensitive matching that translates cleanly on
    /// SQL Server / PostgreSQL / SQLite via EF Core; rely on collation if you need
    /// case-sensitive semantics.
    /// </summary>
    private static Expression BuildStringCall(MemberExpression member, FilterCriteria filter, string methodName)
    {
        if (member.Type != typeof(string))
        {
            throw new ArgumentException(
                $"Operator '{filter.Operator}' on '{filter.Field}' requires a string property.");
        }

        var constant = Constant(member, filter.Value, filter.Field);

        return Expression.Call(
            Expression.Call(member, nameof(string.ToUpper), null),
            methodName,
            null,
            Expression.Call(constant, nameof(string.ToUpper), null));
    }

    private static ConstantExpression Constant(MemberExpression member, string raw, string field) =>
        Expression.Constant(FilterValueParser.Parse(raw, member.Type, field), member.Type);

    private static IReadOnlyList<string> SplitValues(string raw) =>
        raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
}
