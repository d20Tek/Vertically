namespace D20Tek.Vertically.Queries.Pagination;

/// <summary>
/// A provider-agnostic sort instruction. Adapters resolve <paramref name="Field"/> against their
/// own model and order by it in the given <paramref name="Direction"/>.
/// </summary>
/// <param name="Field">The name of the field to sort by.</param>
/// <param name="Direction">The direction to sort in. Defaults to ascending.</param>
public sealed record SortExpression(string Field, SortDirection Direction = SortDirection.Ascending);
