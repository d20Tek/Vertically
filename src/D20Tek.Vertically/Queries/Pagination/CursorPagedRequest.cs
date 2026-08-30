namespace D20Tek.Vertically.Queries.Pagination;

/// <summary>
/// Cursor/keyset paging request. Callers supply an opaque <see cref="Cursor"/> that identifies the
/// position to page from (<c>null</c> for the first page) and a <see cref="PageSize"/>. Adapters
/// interpret the cursor against their own keyset ordering.
/// </summary>
public record CursorPagedRequest : IPagedRequest
{
    /// <summary>The default page size when none is supplied.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>The maximum page size a caller may request.</summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// The opaque cursor identifying the position to page from, or <c>null</c> for the first page.
    /// </summary>
    public string? Cursor { get; init; }

    /// <summary>The number of items per page.</summary>
    public int PageSize { get; init; } = DefaultPageSize;
}
