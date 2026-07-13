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
    /// <summary>The page size ceiling applied when the caller does not pass an explicit one.</summary>
    public const int DefaultMaxPageSize = 200;

    /// <summary>
    /// Applies filtering, sorting, and paging from <paramref name="options"/> to
    /// <paramref name="query"/> and materializes a <see cref="PagedResult{T}"/>.
    /// The requested page size is clamped to <paramref name="maxPageSize"/> so a client
    /// can never request an unbounded page. Throws <see cref="ArgumentException"/> if a
    /// requested field/operator/value is invalid; callers typically map this to a 400.
    /// </summary>
    public static async Task<PagedResult<T>> ApplyQueryOptionsAsync<T>(
        this IQueryable<T> query,
        QueryOptions options,
        CancellationToken cancellationToken = default,
        int maxPageSize = DefaultMaxPageSize)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (maxPageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPageSize), "Max page size must be at least 1.");
        }

        var filtered = query.ApplyQueryOptions(options);

        var totalCount = await filtered.CountAsync(cancellationToken).ConfigureAwait(false);

        var page = options.Page < 1 ? 1 : options.Page;
        var pageSize = Math.Min(options.PageSize < 1 ? 20 : options.PageSize, maxPageSize);

        var items = await filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}
