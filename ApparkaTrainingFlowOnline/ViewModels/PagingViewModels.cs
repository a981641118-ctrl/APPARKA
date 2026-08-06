namespace ApparkaTrainingFlowOnline.ViewModels;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; } = 1;
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int FirstItem => TotalItems == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastItem => Math.Min(Page * PageSize, TotalItems);

    public PagedResult<TDestination> Map<TDestination>(Func<T, TDestination> selector) => new()
    {
        Items = Items.Select(selector).ToList(),
        Page = Page,
        PageSize = PageSize,
        TotalItems = TotalItems,
        TotalPages = TotalPages
    };
}

public sealed class PaginationViewModel
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public string Action { get; init; } = "Index";
    public string? Controller { get; init; }
    public string PageParameter { get; init; } = "page";
    public string? Anchor { get; init; }
    public string ItemLabel { get; init; } = "registros";
    public Dictionary<string, string?> RouteValues { get; init; } = [];
}

public static class ListPageSizes
{
    public const int Standard = 12;
    public const int Actions = 6;
}
