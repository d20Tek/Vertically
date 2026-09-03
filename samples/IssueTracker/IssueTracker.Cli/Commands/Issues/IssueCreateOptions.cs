using IssueTracker.Application.Domain;
using IssueTracker.Application.Features.Issues;
using System.CommandLine;

namespace IssueTracker.Cli.Commands.Issues;

/// <summary>
/// Owns the option definitions for the <c>issue create</c> verb and maps a parsed command line into a
/// <see cref="CreateIssue.Command"/>. Bundling the options with their translation keeps the command class
/// focused on wiring/dispatch (following the same convention as <see cref="IssueListOptions"/>).
/// </summary>
internal sealed class IssueCreateOptions
{
    public Option<string> Title { get; } = new("--title")
    {
        Description = "The issue title (required).",
        Required = true,
    };

    public Option<string?> Description { get; } = new("--description")
    {
        Description = "An optional longer description of the issue.",
    };

    public Option<IssuePriority> Priority { get; } = new("--priority")
    {
        Description = "The issue priority (Low, Medium, High, Critical).",
        DefaultValueFactory = _ => IssuePriority.Medium,
    };

    public Option<string?> Key { get; } = new("--key")
    {
        Description = "An optional explicit issue key (e.g. ISSUE-42). Auto-generated when omitted.",
    };

    /// <summary>Adds every option to <paramref name="command"/>.</summary>
    public void AddTo(Command command)
    {
        command.Add(Title);
        command.Add(Description);
        command.Add(Priority);
        command.Add(Key);
    }

    /// <summary>Builds the <see cref="CreateIssue.Command"/> from the parsed command line.</summary>
    public CreateIssue.Command ToCommand(ParseResult parseResult) => new(
        parseResult.GetValue(Title) ?? string.Empty,
        parseResult.GetValue(Description),
        parseResult.GetValue(Priority),
        parseResult.GetValue(Key));
}
