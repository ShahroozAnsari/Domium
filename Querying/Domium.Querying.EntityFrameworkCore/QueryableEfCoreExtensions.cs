using Domium.Querying;
using Domium.Querying.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Domium.Querying.EntityFrameworkCore;

/// <summary>
/// EF Core-aware convenience wrapper around Domium.Querying's filtering and sorting,
/// adding real async paging via EF Core's CountAsync/ToListAsync.
/// </summary>
public static class QueryableEfCoreExtensions
{
    /// <summary>
    /// Applies filtering, sorting, and paging from <paramref name="options"/> to
    /// <paramref name="query"/> and materializes a <see cref="PagedResult{T}"/>.
    /// Throws <see cref="ArgumentException"/> if a requested field/operator/value is invalid;
    /// callers typically map this to a 400 response.
    /// </summary>
    public static async Task<PagedResult<T>> ApplyQueryOptionsAsync<T>(
        this IQueryable<T> query,
        QueryOptions options,
        CancellationToken cancellationToken = default)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var filtered = query.ApplyQueryOptions(options);

        var totalCount = await filtered.CountAsync(cancellationToken).ConfigureAwait(false);

        var page = options.Page < 1 ? 1 : options.Page;
        var pageSize = options.PageSize < 1 ? 20 : options.PageSize;

        var items = await filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}
