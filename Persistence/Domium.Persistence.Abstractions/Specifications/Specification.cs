using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Domium.Persistence.Abstractions.Specifications;

public abstract class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = new();

    public Expression<Func<T, bool>>? Criteria { get; protected set; }

    public Expression<Func<T, object>>? OrderBy { get; protected set; }

    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }

    public Expression<Func<T, bool>>? CursorCriteria { get; protected set; }

    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;

    public int? Skip { get; protected set; }

    public int? Take { get; protected set; }

    public bool IsPagingEnabled => Skip.HasValue || Take.HasValue;

    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    protected void AddCursor(Expression<Func<T, bool>> cursor)
    {
        CursorCriteria = cursor;
    }

    protected void AddInclude(Expression<Func<T, object>> include)
    {
        _includes.Add(include);
    }

    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy)
    {
        OrderBy = orderBy;
    }

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderBy)
    {
        OrderByDescending = orderBy;
    }

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    protected void ApplyLimit(int limit)
    {
        Take = limit;
    }
}
