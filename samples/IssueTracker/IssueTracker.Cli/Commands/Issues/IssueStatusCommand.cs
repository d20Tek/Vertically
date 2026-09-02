using IssueTracker.Application.Domain;
using IssueTracker.Application.Features.Issues;
using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace IssueTracker.Cli.Commands.Issues;

/// <summary>
/// The <c>issue status</c> verb: resolves the target issue by its friendly <c>Key</c> and dispatches
/// <see cref="ChangeIssueStatus"/> to transition it. Illegal-transition failures are surfaced to stderr
/// with a non-zero exit code by the shared console translation helper.
/// </summary>
internal sealed class IssueStatusCommand : Command
{
    private readonly IHost _host;

    private readonly Argument<string> _key = new("key")
    {
        Description = "The friendly issue key to update (e.g. ISSUE-1).",
    };

    private readonly Option<IssueStatus> _to = new("--to")
    {
        Description = "The status to transition the issue to (Open, InProgress, Resolved, Closed).",
        Required = true,
    };

    public IssueStatusCommand(IHost host) : base("status", "Change an issue's status.")
    {
        _host = host;
        Add(_key);
        Add(_to);
        SetAction((parseResult, cancellationToken) =>
            IssueUpdateHelper.ResolveAndDispatchAsync(
                _host,
                parseResult.GetValue(_key) ?? string.Empty,
                issueId => new ChangeIssueStatus.Command(issueId, parseResult.GetValue(_to)),
                issue => $"Updated {issue.Key} status to {issue.Status}.",
                cancellationToken));
    }
}
