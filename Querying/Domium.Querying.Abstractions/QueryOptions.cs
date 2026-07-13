using System;
using System.Collections.Generic;
using System.Linq;

namespace Domium.Querying.Abstractions;

/// <summary>
/// A single parsed filter condition.
/// </summary>
public sealed class FilterCriteria
{
    public FilterCriteria(string field, FilterOperator @operator, string value)
    {
        Field = field ?? throw new ArgumentNullException(nameof(field));
        Operator = @operator;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Property name, dot-separated for nested paths, for example "Category.Name".</summary>
    public string Field { get; }

    public FilterOperator Operator { get; }

    /// <summary>
    /// Raw value. For <see cref="FilterOperator.In"/> and <see cref="FilterOperator.Between"/>
    /// this is pipe-separated, for example "1|2|3" or "10|100".
    /// </summary>
    public string Value { get; }
}

/// <summary>
/// Generic query options for filtering, sorting, and paging. Bind this as [FromQuery]
/// on any Domium API endpoint.
/// </summary>
public sealed class QueryOptions
{
    /// <summary>
    /// Packed filter syntax: "Field:Operator:Value,Field:Operator:Value".
    /// Example: "Price:Gt:100,Name:Contains:john,Category.Name:Eq:Shoes".
    /// Nested properties use dot notation. In/Between operators take pipe-separated
    /// values: "Id:In:1|2|3", "Price:Between:10|100".
    /// </summary>
    public string? Filters { get; set; }

    /// <summary>One or more comma-separated sort keys, each optionally prefixed with "-" for descending. Example: "Name,-CreatedAt".</summary>
    public string? SortBy { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Parses <see cref="Filters"/> into structured criteria. Malformed segments
    /// (wrong shape, unknown operator name) are silently skipped here; field/operator
    /// validity against a specific entity is checked later, in Domium.Querying, where
    /// the entity's [Filterable] metadata is known.
    /// </summary>
    public IReadOnlyList<FilterCriteria> ParseFilters()
    {
        var list = new List<FilterCriteria>();
        if (string.IsNullOrWhiteSpace(Filters))
        {
            return list;
        }

        foreach (var part in Filters!.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var segments = part.Split(new[] { ':' }, 3);
            if (segments.Length != 3)
            {
                continue;
            }

            if (!Enum.TryParse<FilterOperator>(segments[1], true, out var op))
            {
                continue;
            }

            list.Add(new FilterCriteria(segments[0].Trim(), op, segments[2].Trim()));
        }

        return list;
    }
}
