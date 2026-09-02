using IssueTracker.Cli;
using IssueTracker.Persistence;

// Build the console host (shared Application + Persistence, same issues.db as the API/Web hosts).
using var host = CliHostFactory.Build(args);

// Apply migrations (and deterministic seed) on startup — sample convenience.
// NOTE: Don't do this in production, use a proper migration strategy.
await host.Services.MigrateIssueTrackerAsync();

var rootCommand = host.RegisterCliCommands();
return await rootCommand.Parse(args).InvokeAsync();
