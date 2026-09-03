using D20Tek.Vertically.Queries.Pagination;
using IssueTracker.Application.Domain;

namespace IssueTracker.Web.Components.Pages;

/// <summary>
/// Mutable view-model holding the issue board's filter/sort/page-size selections. Owned by the board
/// page and edited by the <c>IssueFilterBar</c> child component, which raises a change callback so the
/// parent can re-run the <see cref="GetIssues"/> query.
/// </summary>
public sealed class IssueFilterCriteria
{
    /// <summary>Sentinel value used by the assignee selector to mean "no assignee".</summary>
    public const string UnassignedToken = "unassigned";

    /// <summary>Selected status filter (enum name), or empty for all.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Selected priority filter (enum name), or empty for all.</summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>Selected assignee filter (user id, <see cref="UnassignedToken"/>, or empty for all).</summary>
    public string Assignee { get; set; } = string.Empty;

    /// <summary>Sort direction applied to the created date.</summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;

    /// <summary>Number of issues per page.</summary>
    public int PageSize { get; set; } = 10;

    /// <summary>Builds the provider-agnostic filter tree from the current selections (null when none).</summary>
    public FilterGroup? ToFilter()
    {
        var nodes = new List<FilterNode>();

        if (Enum.TryParse<IssueStatus>(Status, out var status))
        {
            nodes.Add(new FilterExpression(nameof(Issue.Status), FilterOperator.Equals, status));
        }

        if (Enum.TryParse<IssuePriority>(Priority, out var priority))
        {
            nodes.Add(new FilterExpression(nameof(Issue.Priority), FilterOperator.Equals, priority));
        }

        if (Assignee == UnassignedToken)
        {
            nodes.Add(new FilterExpression(nameof(Issue.AssigneeId), FilterOperator.Equals, null));
        }
        else if (Guid.TryParse(Assignee, out var assigneeId))
        {
            nodes.Add(new FilterExpression(nameof(Issue.AssigneeId), FilterOperator.Equals, assigneeId));
        }

        return nodes.Count == 0 ? null : new FilterGroup(FilterLogic.And, nodes);
    }
}
