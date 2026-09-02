using D20Tek.Vertically.Queries.Pagination;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Features.Issues;
using System.CommandLine;

namespace IssueTracker.Cli.Commands;

/// <summary>
/// Owns the shared option definitions for the <c>issue list</c> verb and maps a parsed command line into
/// a <see cref="GetIssues.Query"/>. Bundling the options with their translation keeps the command class
/// focused on wiring/dispatch and centralizes the filter/sort mapping for reuse.
/// </summary>
internal sealed class IssueListOptions
{
    public Option<IssueStatus?> Status { get; } = new("--status")
    {
        Description = "Filter by status (e.g. Open, InProgress, Closed).",
    };

    public Option<IssuePriority?> Priority { get; } = new("--priority")
    {
        Description = "Filter by priority (e.g. Low, Medium, High).",
    };

    public Option<string?> Assignee { get; } = new("--assignee")
    {
        Description = "Filter by assignee user id, or 'unassigned'.",
    };

    public Option<string> Sort { get; } = new("--sort")
    {
        Description = "Sort by created date: 'created' (ascending) or '-created' (descending).",
        DefaultValueFactory = _ => "-created",
    };

    public Option<int> Page { get; } = new("--page")
    {
        Description = "One-based page number.",
        DefaultValueFactory = _ => 1,
    };

    public Option<int> Size { get; } = new("--size")
    {
        Description = "Number of issues per page.",
        DefaultValueFactory = _ => 10,
    };

    /// <summary>Adds every option to <paramref name="command"/>.</summary>
    public void AddTo(Command command)
    {
        command.Add(Status);
        command.Add(Priority);
        command.Add(Assignee);
        command.Add(Sort);
        command.Add(Page);
        command.Add(Size);
    }

    /// <summary>Builds the <see cref="GetIssues.Query"/> from the parsed command line.</summary>
    public GetIssues.Query ToQuery(ParseResult parseResult) => new()
    {
        PageNumber = parseResult.GetValue(Page),
        PageSize = parseResult.GetValue(Size),
        Sorts = [BuildSort(parseResult.GetValue(Sort))],
        Filter = BuildFilter(
            parseResult.GetValue(Status),
            parseResult.GetValue(Priority),
            parseResult.GetValue(Assignee)),
    };

    private static SortExpression BuildSort(string? sort)
    {
        var direction = sort is not null && sort.StartsWith('-')
            ? SortDirection.Descending
            : SortDirection.Ascending;
        return new SortExpression(nameof(Issue.CreatedUtc), direction);
    }

    private static FilterGroup? BuildFilter(IssueStatus? status, IssuePriority? priority, string? assignee)
    {
        var nodes = new List<FilterNode>();

        if (status is not null)
        {
            nodes.Add(new FilterExpression(nameof(Issue.Status), FilterOperator.Equals, status));
        }

        if (priority is not null)
        {
            nodes.Add(new FilterExpression(nameof(Issue.Priority), FilterOperator.Equals, priority));
        }

        if (string.Equals(assignee, "unassigned", StringComparison.OrdinalIgnoreCase))
        {
            nodes.Add(new FilterExpression(nameof(Issue.AssigneeId), FilterOperator.Equals, null));
        }
        else if (Guid.TryParse(assignee, out var assigneeId))
        {
            nodes.Add(new FilterExpression(nameof(Issue.AssigneeId), FilterOperator.Equals, assigneeId));
        }

        return nodes.Count == 0 ? null : new FilterGroup(FilterLogic.And, nodes);
    }
}
