using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Domium.Persistence.EntityFrameworkCore.Specifications;

/// <summary>
/// Describes an EF Core query shape.
/// </summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }

    IReadOnlyCollection<Expression<Func<T, object>>> Includes { get; }

    Expression<Func<T, object>>? OrderBy { get; }

    Expression<Func<T, object>>? OrderByDescending { get; }

    int? Skip { get; }

    int? Take { get; }

    bool IsPagingEnabled { get; }
}
