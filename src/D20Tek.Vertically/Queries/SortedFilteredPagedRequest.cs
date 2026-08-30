namespace D20Tek.Vertically.Queries;

/// <summary>
/// An offset paging request that additionally carries provider-agnostic sort and filter
/// instructions. Callers who only need paging can use <see cref="PagedRequest"/> directly.
/// </summary>
public record SortedFilteredPagedRequest : PagedRequest
{
    /// <summary>The ordered set of sort instructions to apply.</summary>
    public IReadOnlyList<SortExpression> Sorts { get; init; } = [];

    /// <summary>The set of filter instructions to apply.</summary>
    public IReadOnlyList<FilterExpression> Filters { get; init; } = [];
}
