using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Domium.Persistence.Abstractions.Specifications;

public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }

    Expression<Func<T, object>>? OrderBy { get; }

    Expression<Func<T, object>>? OrderByDescending { get; }

    Expression<Func<T, bool>>? CursorCriteria { get; }

    int? Take { get; }

    bool IsPagingEnabled { get; }
}

