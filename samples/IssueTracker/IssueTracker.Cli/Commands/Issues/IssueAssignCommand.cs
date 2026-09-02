using IssueTracker.Application.Features.Issues;
using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace IssueTracker.Cli.Commands.Issues;

/// <summary>
/// The <c>issue assign</c> verb: resolves the target issue by its friendly <c>Key</c> and dispatches
/// <see cref="AssignIssue"/> to set its assignee. Business-rule failures (e.g. a closed issue cannot be
/// assigned, or an unknown user) are surfaced to stderr with a non-zero exit code by the shared console
/// translation helper.
/// </summary>
internal sealed class IssueAssignCommand : Command
{
    private readonly IHost _host;

    private readonly Argument<string> _key = new("key")
    {
        Description = "The friendly issue key to assign (e.g. ISSUE-1).",
    };

    private readonly Option<Guid> _user = new("--user")
    {
        Description = "The id of the user to assign the issue to.",
        Required = true,
    };

    public IssueAssignCommand(IHost host) : base("assign", "Assign an issue to a user.")
    {
        _host = host;
        Add(_key);
        Add(_user);
        SetAction((parseResult, cancellationToken) =>
            IssueUpdateHelper.ResolveAndDispatchAsync(
                _host,
                parseResult.GetValue(_key) ?? string.Empty,
                issueId => new AssignIssue.Command(issueId, parseResult.GetValue(_user)),
                issue => $"Assigned {issue.Key} to {issue.AssigneeId}.",
                cancellationToken));
    }
}
