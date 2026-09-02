using IssueTracker.Cli;
using IssueTracker.Cli.Commands;
using IssueTracker.Persistence;
using System.CommandLine;

// Build the console host (shared Application + Persistence, same issues.db as the API/Web hosts).
using var host = CliHostFactory.Build(args);

// Apply migrations (and deterministic seed) on startup — sample convenience.
// NOTE: Don't do this in production, use a proper migration strategy.
await host.Services.MigrateIssueTrackerAsync();

// Build the System.CommandLine tree. Each verb resolves feature handlers from a per-invocation DI
// scope, so every dispatch gets its own IIssueDbContext (mirrors per-request/per-render scoping).
var issueCommand = new Command("issue", "Work with issues.")
{
    new IssueListCommand(host),
    new IssueShowCommand(host),
};

var rootCommand = new RootCommand("IssueTracker CLI — a console host over the shared Vertically slices.")
{
    issueCommand,
};

return await rootCommand.Parse(args).InvokeAsync();
