using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Domium.Persistence.Abstractions.Specifications;

/// <summary>
/// Base implementation for query specifications.
/// </summary>
/// <typeparam name="T">The aggregate or entity type being queried.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = new();

    protected Specification()
    {
    }

    protected Specification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));
    }

    public Expression<Func<T, bool>>? Criteria { get; private set; }

    public IReadOnlyCollection<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();

    public Expression<Func<T, object>>? OrderBy { get; private set; }

    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    public int? Skip { get; private set; }

    public int? Take { get; private set; }

    public bool IsPagingEnabled => Skip.HasValue && Take.HasValue;

    protected void ApplyCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));
    }

    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        if (includeExpression == null)
        {
            throw new ArgumentNullException(nameof(includeExpression));
        }

        _includes.Add(includeExpression);
    }

    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression ?? throw new ArgumentNullException(nameof(orderByExpression));
        OrderByDescending = null;
    }

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression ?? throw new ArgumentNullException(nameof(orderByDescendingExpression));
        OrderBy = null;
    }

    protected void ApplyPaging(int skip, int take)
    {
        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be zero or greater.");
        }

        if (take <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be greater than zero.");
        }

        Skip = skip;
        Take = take;
    }
}
