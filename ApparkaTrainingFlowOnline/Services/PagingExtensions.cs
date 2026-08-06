using ApparkaTrainingFlowOnline.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ApparkaTrainingFlowOnline.Services;

public static class PagingExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize = ListPageSizes.Standard,
        CancellationToken cancellationToken = default)
    {
        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        var safePage = Math.Clamp(page, 1, totalPages);
        var items = totalItems == 0
            ? []
            : await query.Skip((safePage - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            Page = safePage,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }
}
