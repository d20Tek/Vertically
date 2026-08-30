namespace D20Tek.Vertically.Queries;

/// <summary>
/// Offset-based paging request. Callers specify a one-based <see cref="PageNumber"/> and a
/// <see cref="PageSize"/>; adapters use <see cref="Skip"/>/<see cref="Take"/> to page a data set.
/// </summary>
public record PagedRequest : IPagedRequest
{
    /// <summary>The default page number when none is supplied.</summary>
    public const int DefaultPageNumber = 1;

    /// <summary>The default page size when none is supplied.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>The maximum page size a caller may request.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The one-based page number to retrieve.</summary>
    public int PageNumber { get; init; } = DefaultPageNumber;

    /// <summary>The number of items per page.</summary>
    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>The number of items to skip to reach the requested page.</summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>The number of items to take for the requested page.</summary>
    public int Take => PageSize;
}
