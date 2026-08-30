namespace D20Tek.Vertically.Queries.Pagination;

/// <summary>
/// Represents a single page of items produced by cursor/keyset paging, along with the opaque
/// cursors needed to navigate forward and backward. Query handlers return
/// <c>Result&lt;CursorPageOf&lt;T&gt;&gt;</c>.
/// </summary>
/// <typeparam name="T">The type of the items in the page.</typeparam>
public sealed record CursorPageOf<T>
{
    /// <summary>The items on this page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>The number of items per page that was requested.</summary>
    public int PageSize { get; init; }

    /// <summary>The opaque cursor for the next page, or <c>null</c> when there is no next page.</summary>
    public string? NextCursor { get; init; }

    /// <summary>The opaque cursor for the previous page, or <c>null</c> when there is no previous page.</summary>
    public string? PreviousCursor { get; init; }

    /// <summary>Whether a next page exists, derived from the presence of a <see cref="NextCursor"/>.</summary>
    public bool HasNext => NextCursor is not null;

    /// <summary>Whether a previous page exists, derived from the presence of a <see cref="PreviousCursor"/>.</summary>
    public bool HasPrevious => PreviousCursor is not null;

    /// <summary>
    /// Creates a <see cref="CursorPageOf{T}"/> deriving the page size from the originating request.
    /// </summary>
    /// <param name="items">The items on the page.</param>
    /// <param name="request">The request that produced this page.</param>
    /// <param name="nextCursor">The cursor for the next page, or <c>null</c> when none.</param>
    /// <param name="previousCursor">The cursor for the previous page, or <c>null</c> when none.</param>
    public static CursorPageOf<T> Create(
        IReadOnlyList<T> items,
        CursorPagedRequest request,
        string? nextCursor = null,
        string? previousCursor = null) =>
        new()
        {
            Items = items,
            PageSize = request.PageSize,
            NextCursor = nextCursor,
            PreviousCursor = previousCursor,
        };

    /// <summary>
    /// Creates an empty page that preserves the page size of the originating request.
    /// </summary>
    /// <param name="request">The request that produced this (empty) page.</param>
    public static CursorPageOf<T> Empty(CursorPagedRequest request) => Create([], request);

    /// <summary>
    /// Projects each item to a new type while preserving this page's cursors and metadata.
    /// </summary>
    /// <typeparam name="TOut">The projected item type.</typeparam>
    /// <param name="selector">The projection applied to each item.</param>
    public CursorPageOf<TOut> Map<TOut>(Func<T, TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var mapped = new TOut[Items.Count];
        for (var i = 0; i < Items.Count; i++)
        {
            mapped[i] = selector(Items[i]);
        }

        return new CursorPageOf<TOut>
        {
            Items = mapped,
            PageSize = PageSize,
            NextCursor = NextCursor,
            PreviousCursor = PreviousCursor,
        };
    }
}
