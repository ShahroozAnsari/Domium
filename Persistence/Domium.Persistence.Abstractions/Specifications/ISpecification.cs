using System;
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
    Expression<Func<T, bool>> Criteria { get; }
}
