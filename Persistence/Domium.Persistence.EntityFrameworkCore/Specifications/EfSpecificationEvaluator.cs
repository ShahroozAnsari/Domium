using Domium.Persistence.Abstractions.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Domium.Persistence.EntityFrameworkCore.Specifications;

public static class EfSpecificationEvaluator
{
    public static IQueryable<T> GetQuery<T>(
        IQueryable<T> source,
        ISpecification<T> specification)
        where T : class
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (specification == null)
        {
            throw new ArgumentNullException(nameof(specification));
        }

        var query = source;

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        if (specification.CursorCriteria is not null)
        {
            query = query.Where(specification.CursorCriteria);
        }

        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        if (specification.IsPagingEnabled)
        {
            query = query.Take(specification.Take!.Value);
        }

        return query;
    }
}