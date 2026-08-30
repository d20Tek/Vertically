namespace D20Tek.Vertically.Queries;

/// <summary>
/// Represents a single page of items along with the metadata needed to navigate a paged data set.
/// Query handlers return <c>Result&lt;PageOf&lt;T&gt;&gt;</c>.
/// </summary>
/// <typeparam name="T">The type of the items in the page.</typeparam>
public sealed record PageOf<T>
{
    /// <summary>The items on this page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>The one-based number of this page.</summary>
    public int PageNumber { get; init; }

    /// <summary>The number of items per page.</summary>
    public int PageSize { get; init; }

    /// <summary>The total number of items across all pages.</summary>
    public long TotalCount { get; init; }

    /// <summary>The total number of pages given <see cref="TotalCount"/> and <see cref="PageSize"/>.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Whether a previous page exists.</summary>
    public bool HasPrevious => PageNumber > 1;

    /// <summary>Whether a next page exists.</summary>
    public bool HasNext => PageNumber < TotalPages;

    /// <summary>
    /// Creates a <see cref="PageOf{T}"/> deriving page metadata from the originating request.
    /// </summary>
    /// <param name="items">The items on the page.</param>
    /// <param name="request">The request that produced this page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    public static PageOf<T> Create(IReadOnlyList<T> items, PagedRequest request, long totalCount) =>
        new()
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
}
