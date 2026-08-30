namespace D20Tek.Vertically.Queries;

/// <summary>
/// The direction a <see cref="SortExpression"/> orders results.
/// </summary>
public enum SortDirection
{
    /// <summary>Order ascending (A-Z, 0-9).</summary>
    Ascending = 0,

    /// <summary>Order descending (Z-A, 9-0).</summary>
    Descending = 1,
}
