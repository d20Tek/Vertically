using D20Tek.Vertically.Queries.Pagination;
using IssueTracker.Application.Features.Issues;

namespace IssueTracker.Cli.Rendering;

/// <summary>
/// Presentation helper that renders <see cref="IssueResponse"/> data as plain text for the CLI verbs —
/// a paged table for list output and an aligned detail block for show output. Keeping this separate from
/// the command wiring lets each command class focus on parsing/dispatch rather than formatting.
/// </summary>
internal static class IssuePresenter
{
    private const string ShortIdPlaceholder = "-";

    /// <summary>Renders a page of issues as a plain-text table with a paging footer.</summary>
    public static string RenderList(PageOf<IssueResponse> page)
    {
        var headers = new[] { "Id", "Key", "Title", "Status", "Priority", "Assignee", "Created" };
        var rows = page.Items
            .Select(i => (IReadOnlyList<string>)new[]
            {
                ShortId(i.Id),
                i.Key,
                i.Title,
                i.Status.ToString(),
                i.Priority.ToString(),
                i.AssigneeId is { } assignee ? ShortId(assignee) : ShortIdPlaceholder,
                i.CreatedUtc.ToString("yyyy-MM-dd HH:mm"),
            })
            .ToList();

        var table = ConsoleFormatter.Table(headers, rows, "No issues found.");
        if (page.Items.Count == 0)
        {
            return table;
        }

        var footer = $"Page {page.PageNumber} of {Math.Max(page.TotalPages, 1)} — {page.TotalCount} total";
        return $"{table}{Environment.NewLine}{Environment.NewLine}{footer}";
    }

    /// <summary>Renders a single issue as an aligned <c>Label : Value</c> detail block.</summary>
    public static string RenderDetail(IssueResponse issue) =>
        ConsoleFormatter.Detail(
        [
            ("Key", issue.Key),
            ("Id", issue.Id.ToString()),
            ("Title", issue.Title),
            ("Description", string.IsNullOrWhiteSpace(issue.Description) ? "-" : issue.Description),
            ("Status", issue.Status.ToString()),
            ("Priority", issue.Priority.ToString()),
            ("Assignee", issue.AssigneeId?.ToString() ?? "Unassigned"),
            ("Created", issue.CreatedUtc.ToString("yyyy-MM-dd HH:mm")),
            ("Updated", issue.UpdatedUtc.ToString("yyyy-MM-dd HH:mm")),
        ]);

    private static string ShortId(Guid id) => id.ToString()[..8];
}
