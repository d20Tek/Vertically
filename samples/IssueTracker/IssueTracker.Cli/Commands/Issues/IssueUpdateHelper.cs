using D20Tek.Functional.Async;
using D20Tek.Vertically;
using IssueTracker.Application.Features.Issues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IssueTracker.Cli.Commands.Issues;

/// <summary>
/// Shared execution helper for issue-mutating verbs. Every such verb identifies its target by the
/// friendly <c>Key</c> (e.g. <c>ISSUE-1</c>), so this resolves the key to the issue via
/// <see cref="GetIssueByKey"/> and, on success, dispatches the mutation command. The command is built
/// either from the resolved issue id (for simple mutations) or from the full <see cref="IssueResponse"/>
/// (so verbs like <c>edit</c> can default omitted fields to the issue's current values). The lookup and
/// command share a single DI scope so they use the same <c>IIssueDbContext</c>, and a not-found key
/// short-circuits to the shared console translation (stderr + non-zero exit code).
/// </summary>
internal static class IssueUpdateHelper
{
    public static Task<int> ResolveAndDispatchAsync<TCommand>(
        IHost host,
        string key,
        Func<Guid, TCommand> buildCommand,
        Func<IssueResponse, string> render,
        CancellationToken cancellationToken)
        where TCommand : ICommand<IssueResponse> =>
        ResolveAndDispatchAsync(host, key, issue => buildCommand(issue.Id), render, cancellationToken);

    public static async Task<int> ResolveAndDispatchAsync<TCommand>(
        IHost host,
        string key,
        Func<IssueResponse, TCommand> buildCommand,
        Func<IssueResponse, string> render,
        CancellationToken cancellationToken)
        where TCommand : ICommand<IssueResponse>
    {
        using var scope = host.Services.CreateScope();
        var provider = scope.ServiceProvider;

        var lookup = provider.GetRequiredService<IQueryHandler<GetIssueByKey.Query, IssueResponse>>();

        var result = await lookup.HandleAsync(new GetIssueByKey.Query(key), cancellationToken)
            .BindAsync(async issue =>
            {
                var handler = provider.GetRequiredService<ICommandHandler<TCommand, IssueResponse>>();
                return await handler.HandleAsync(buildCommand(issue), cancellationToken);
            });

        return result.ToConsole(render);
    }
}
