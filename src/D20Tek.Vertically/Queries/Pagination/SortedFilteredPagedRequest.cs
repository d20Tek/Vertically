namespace D20Tek.Vertically.Queries.Pagination;

/// <summary>
/// An offset paging request that additionally carries provider-agnostic sort and filter
/// instructions. Callers who only need paging can use <see cref="PagedRequest"/> directly.
/// </summary>
public record SortedFilteredPagedRequest : PagedRequest
{
    /// <summary>The ordered set of sort instructions to apply.</summary>
    public IReadOnlyList<SortExpression> Sorts { get; init; } = [];

    /// <summary>
    /// The root of the filter tree to apply, or <c>null</c> when no filtering is requested.
    /// Use <see cref="FilterGroup"/> to combine expressions with AND/OR and arbitrary nesting.
    /// </summary>
    public FilterGroup? Filter { get; init; }
}
