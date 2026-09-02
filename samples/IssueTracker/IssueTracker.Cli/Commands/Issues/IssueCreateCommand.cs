using D20Tek.Vertically;
using IssueTracker.Application.Features.Issues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace IssueTracker.Cli.Commands.Issues;

/// <summary>
/// The <c>issue create</c> verb: dispatches <see cref="CreateIssue"/> to add a new issue (options owned by
/// <see cref="IssueCreateOptions"/>), then prints a confirmation with the generated <c>Key</c>. Pipeline
/// validation failures are surfaced to stderr with a non-zero exit code by the shared console translation
/// helper. Each invocation resolves the handler from a fresh DI scope so it gets its own
/// <c>IIssueDbContext</c>.
/// </summary>
internal sealed class IssueCreateCommand : Command
{
    private readonly IHost _host;
    private readonly IssueCreateOptions _options = new();

    public IssueCreateCommand(IHost host) : base("create", "Create a new issue.")
    {
        _host = host;
        _options.AddTo(this);
        SetAction((parseResult, cancellationToken) =>
            DispatchAsync(_options.ToCommand(parseResult), cancellationToken));
    }

    private async Task<int> DispatchAsync(CreateIssue.Command command, CancellationToken cancellationToken)
    {
        using var scope = _host.Services.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<CreateIssue.Command, IssueResponse>>();

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToConsole(issue => $"Created {issue.Key} ({issue.Id}).");
    }
}
