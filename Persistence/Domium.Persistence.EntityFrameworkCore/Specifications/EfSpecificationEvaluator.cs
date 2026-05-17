using Microsoft.EntityFrameworkCore;

namespace Domium.Persistence.EntityFrameworkCore.Specifications;

/// <summary>
/// Applies Domium specifications to EF Core queries.
/// </summary>
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

        query = specification.Includes.Aggregate(
            query,
            (current, include) => current.Include(include));

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
            query = query.Skip(specification.Skip!.Value).Take(specification.Take!.Value);
        }

        return query;
    }
}
