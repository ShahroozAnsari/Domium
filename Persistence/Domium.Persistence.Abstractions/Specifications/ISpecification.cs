using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Domium.Persistence.Abstractions.Specifications;

/// <summary>
/// Describes query criteria that can be translated by repository implementations.
/// </summary>
/// <typeparam name="T">The aggregate or entity type being queried.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the predicate used to filter matching items.
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Gets related object expressions that repository implementations may eagerly load.
    /// </summary>
    IReadOnlyCollection<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the ascending ordering expression.
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Gets the descending ordering expression.
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Gets the number of items to skip when paging is enabled.
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Gets the number of items to take when paging is enabled.
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets a value indicating whether paging is enabled.
    /// </summary>
    bool IsPagingEnabled { get; }
}
