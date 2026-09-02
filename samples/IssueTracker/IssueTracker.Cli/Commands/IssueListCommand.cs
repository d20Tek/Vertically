using D20Tek.Vertically;
using D20Tek.Vertically.Queries.Pagination;
using IssueTracker.Application.Features.Issues;
using IssueTracker.Cli.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace IssueTracker.Cli.Commands;

/// <summary>
/// The <c>issue list</c> verb: dispatches <see cref="GetIssues"/> with optional status, priority,
/// assignee, sort, and paging options (owned by <see cref="IssueListOptions"/>) and renders the resulting
/// page via <see cref="IssuePresenter"/>. Each invocation resolves the handler from a fresh DI scope so
/// it gets its own <c>IIssueDbContext</c>.
/// </summary>
internal sealed class IssueListCommand : Command
{
    private readonly IHost _host;
    private readonly IssueListOptions _options = new();

    public IssueListCommand(IHost host) : base("list", "List issues with optional filtering, sorting, and paging.")
    {
        _host = host;
        _options.AddTo(this);
        SetAction((parseResult, cancellationToken) =>
            DispatchAsync(_options.ToQuery(parseResult), cancellationToken));
    }

    private async Task<int> DispatchAsync(GetIssues.Query query, CancellationToken cancellationToken)
    {
        using var scope = _host.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetIssues.Query, PageOf<IssueResponse>>>();

        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToConsole(IssuePresenter.RenderList);
    }
}
