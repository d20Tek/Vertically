using IssueTracker.Application.Domain;
using IssueTracker.Application.Features.Issues;
using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace IssueTracker.Cli.Commands.Issues;

/// <summary>
/// The <c>issue priority</c> verb: resolves the target issue by its friendly <c>Key</c> and dispatches
/// <see cref="ChangeIssuePriority"/> to reprioritize it. Business-rule failures are surfaced to stderr
/// with a non-zero exit code by the shared console translation helper.
/// </summary>
internal sealed class IssuePriorityCommand : Command
{
    private readonly IHost _host;

    private readonly Argument<string> _key = new("key")
    {
        Description = "The friendly issue key to update (e.g. ISSUE-1).",
    };

    private readonly Option<IssuePriority> _to = new("--to")
    {
        Description = "The priority to set the issue to (Low, Medium, High, Critical).",
        Required = true,
    };

    public IssuePriorityCommand(IHost host) : base("priority", "Change an issue's priority.")
    {
        _host = host;
        Add(_key);
        Add(_to);
        SetAction((parseResult, cancellationToken) =>
            IssueUpdateHelper.ResolveAndDispatchAsync(
                _host,
                parseResult.GetValue(_key) ?? string.Empty,
                issueId => new ChangeIssuePriority.Command(issueId, parseResult.GetValue(_to)),
                issue => $"Updated {issue.Key} priority to {issue.Priority}.",
                cancellationToken));
    }
}
