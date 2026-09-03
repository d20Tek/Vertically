using IssueTracker.Application.Features.Issues;
using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace IssueTracker.Cli.Commands.Issues;

/// <summary>
/// The <c>issue edit</c> verb: resolves the target issue by its friendly <c>Key</c> and dispatches
/// <see cref="EditIssueDetails"/> to update its title and/or description. Options that are omitted keep
/// the issue's current value (resolved via <see cref="GetIssueByKey"/>), so callers can change just one
/// field. Validation failures are surfaced to stderr with a non-zero exit code by the shared console
/// translation helper.
/// </summary>
internal sealed class IssueEditCommand : Command
{
    private readonly IHost _host;

    private readonly Argument<string> _key = new("key")
    {
        Description = "The friendly issue key to edit (e.g. ISSUE-1).",
    };

    private readonly Option<string> _title = new("--title")
    {
        Description = "The new title. Omit to keep the current title.",
    };

    private readonly Option<string> _description = new("--description")
    {
        Description = "The new description. Omit to keep the current description.",
    };

    public IssueEditCommand(IHost host) : base("edit", "Edit an issue's title and/or description.")
    {
        _host = host;
        Add(_key);
        Add(_title);
        Add(_description);
        SetAction((parseResult, cancellationToken) =>
            IssueUpdateHelper.ResolveAndDispatchAsync(
                _host,
                parseResult.GetValue(_key) ?? string.Empty,
                issue => new EditIssueDetails.Command(
                    issue.Id,
                    parseResult.GetResult(_title) is null ? issue.Title : parseResult.GetValue(_title) ?? string.Empty,
                    parseResult.GetResult(_description) is null ? issue.Description : parseResult.GetValue(_description)),
                issue => $"Updated {issue.Key}.",
                cancellationToken));
    }
}
