namespace D20Tek.Vertically.Queries.Pagination;

/// <summary>
/// A cursor paging request that additionally carries provider-agnostic sort and filter
/// instructions. Cursor paging relies on a stable ordering, so callers should supply
/// <see cref="Sorts"/> that produce a deterministic keyset. Callers who only need cursor paging can
/// use <see cref="CursorPagedRequest"/> directly.
/// </summary>
public record SortedFilteredCursorPagedRequest : CursorPagedRequest
{
    /// <summary>The ordered set of sort instructions to apply.</summary>
    public IReadOnlyList<SortExpression> Sorts { get; init; } = [];

    /// <summary>
    /// The root of the filter tree to apply, or <c>null</c> when no filtering is requested.
    /// Use <see cref="FilterGroup"/> to combine expressions with AND/OR and arbitrary nesting.
    /// </summary>
    public FilterGroup? Filter { get; init; }
}
