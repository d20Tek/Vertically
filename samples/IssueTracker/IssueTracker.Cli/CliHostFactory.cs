using IssueTracker.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IssueTracker.Cli;

/// <summary>
/// Builds the console <see cref="IHost"/> for the CLI, mirroring the API and Web hosts' split
/// registration: handler registration comes from the Application layer, behavior policy is a host
/// decision, and persistence points at the same shared <c>issues.db</c>. The CLI surfaces failures as
/// stderr messages + exit codes, and enables the full logging/exception-to-result/validation pipeline.
/// </summary>
internal static class CliHostFactory
{
    public static IHost Build(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            // Anchor the content root to the app's base directory so appsettings.json is found
            // regardless of the current working directory (e.g. `dotnet run` from the repo root).
            ContentRootPath = AppContext.BaseDirectory,
        });

        // Console logging so pipeline behaviors surface diagnostics in the terminal.
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // Shared, repo-relative SQLite database so every host reads/writes the same data.
        var connectionString = builder.Configuration.GetConnectionString("IssueTracker")
            ?? "Data Source={SharedDataDir}/issues.db";

        // Handler registration lives in the Application layer; behavior policy is a host decision.
        builder.Services.AddIssueTrackerApplication(behaviors => behaviors
            .AddExceptionToResult()
            .AddLogging()
            .AddValidation());
        builder.Services.AddIssueTrackerPersistence(connectionString);

        return builder.Build();
    }
}
