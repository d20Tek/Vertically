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

    /// <summary>
    /// Creates an empty page that preserves the paging metadata of the originating request.
    /// </summary>
    /// <param name="request">The request that produced this (empty) page.</param>
    public static PageOf<T> Empty(PagedRequest request) => Create([], request, totalCount: 0);

    /// <summary>
    /// Projects each item to a new type while preserving this page's navigation metadata.
    /// </summary>
    /// <typeparam name="TOut">The projected item type.</typeparam>
    /// <param name="selector">The projection applied to each item.</param>
    public PageOf<TOut> Map<TOut>(Func<T, TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var mapped = new TOut[Items.Count];
        for (var i = 0; i < Items.Count; i++)
        {
            mapped[i] = selector(Items[i]);
        }

        return new PageOf<TOut>
        {
            Items = mapped,
            PageNumber = PageNumber,
            PageSize = PageSize,
            TotalCount = TotalCount,
        };
    }
}
