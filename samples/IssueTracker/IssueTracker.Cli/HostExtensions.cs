using IssueTracker.Cli.Commands.Issues;
using IssueTracker.Cli.Commands.Users;
using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace IssueTracker.Cli;

internal static class HostExtensions
{
    public static RootCommand RegisterCliCommands(this IHost host)
    {
        // Build the System.CommandLine tree. Each verb resolves feature handlers from a per-invocation DI
        // scope, so every dispatch gets its own IIssueDbContext (mirrors per-request/per-render scoping).
        var issueCommand = new Command("issue", "Work with issues.")
        {
            new IssueListCommand(host),
            new IssueShowCommand(host),
            new IssueCreateCommand(host),
            new IssueAssignCommand(host),
            new IssueStatusCommand(host),
            new IssuePriorityCommand(host),
            new IssueEditCommand(host),
        };

        var userCommand = new Command("user", "Work with users.")
        {
            new UserListCommand(host),
            new UserCreateCommand(host),
        };

        return new RootCommand("IssueTracker CLI — a console host over the shared Vertically slices.")
        {
            issueCommand,
            userCommand,
        };
    }
}
