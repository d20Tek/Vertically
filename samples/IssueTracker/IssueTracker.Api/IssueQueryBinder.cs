using D20Tek.Vertically.Queries.Pagination;
using IssueTracker.Application.Features.Issues;

namespace IssueTracker.Api;

/// <summary>
/// Builds a <see cref="GetIssues.Query"/> from the request query string. Supports paging
/// (<c>pageNumber</c>, <c>pageSize</c>), sorting (<c>sort=field:desc,field2</c>), and simple
/// equality filters (<c>filter=field:value</c>, repeatable; combined with AND). Unknown fields are
/// left for the slice's validator to reject.
/// </summary>
internal static class IssueQueryBinder
{
    public static GetIssues.Query Bind(IQueryCollection query)
    {
        var pageNumber = ParseInt(query["pageNumber"], PagedRequest.DefaultPageNumber);
        var pageSize = ParseInt(query["pageSize"], PagedRequest.DefaultPageSize);

        return new GetIssues.Query
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Sorts = ParseSorts(query["sort"]),
            Filter = ParseFilter(query["filter"]),
        };
    }

    private static int ParseInt(string? value, int fallback) =>
        int.TryParse(value, out var parsed) ? parsed : fallback;

    private static IReadOnlyList<SortExpression> ParseSorts(IEnumerable<string?> values)
    {
        var sorts = new List<SortExpression>();
        foreach (var raw in values.SelectMany(SplitCsv))
        {
            var parts = raw.Split(':', 2, StringSplitOptions.TrimEntries);
            var direction = parts.Length == 2 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? SortDirection.Descending
                : SortDirection.Ascending;
            sorts.Add(new SortExpression(parts[0], direction));
        }

        return sorts;
    }

    private static FilterGroup? ParseFilter(IEnumerable<string?> values)
    {
        var nodes = new List<FilterNode>();
        foreach (var raw in values.SelectMany(SplitCsv))
        {
            var parts = raw.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2) continue;

            nodes.Add(new FilterExpression(parts[0], FilterOperator.Equals, parts[1]));
        }

        return nodes.Count == 0 ? null : new FilterGroup(FilterLogic.And, nodes);
    }

    private static IEnumerable<string> SplitCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
