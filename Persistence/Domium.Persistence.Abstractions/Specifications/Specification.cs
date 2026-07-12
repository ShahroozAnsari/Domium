using System;
using System.Linq.Expressions;

namespace Domium.Persistence.Abstractions.Specifications;

public abstract class Specification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; protected set; }

    public Expression<Func<T, object>>? OrderBy { get; protected set; }

    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }

    public Expression<Func<T, bool>>? CursorCriteria { get; protected set; }

    public int? Take { get; protected set; }

    public bool IsPagingEnabled => Take.HasValue;


    protected void AddCriteria(
        Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }


    protected void AddCursor(
        Expression<Func<T, bool>> cursor)
    {
        CursorCriteria = cursor;
    }


    protected void ApplyOrderBy(
        Expression<Func<T, object>> orderBy)
    {
        OrderBy = orderBy;
    }


    protected void ApplyOrderByDescending(
        Expression<Func<T, object>> orderBy)
    {
        OrderByDescending = orderBy;
    }


    protected void ApplyLimit(int limit)
    {
        Take = limit;
    }
}