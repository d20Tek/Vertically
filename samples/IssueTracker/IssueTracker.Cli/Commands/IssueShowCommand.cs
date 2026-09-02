using D20Tek.Vertically;
using IssueTracker.Application.Features.Issues;
using IssueTracker.Cli.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace IssueTracker.Cli.Commands;

/// <summary>
/// The <c>issue show</c> verb: dispatches <see cref="GetIssueByKey"/> for the supplied friendly key
/// (e.g. <c>ISSUE-1</c>) and renders a plain-text detail block via <see cref="IssuePresenter"/>. A
/// not-found result is written to stderr with a non-zero exit code by the shared console translation
/// helper.
/// </summary>
internal sealed class IssueShowCommand : Command
{
    private readonly IHost _host;
    private readonly Argument<string> _key = new("key")
    {
        Description = "The friendly issue key to display (e.g. ISSUE-1).",
    };

    public IssueShowCommand(IHost host) : base("show", "Show a single issue's details by its key.")
    {
        _host = host;
        Add(_key);
        SetAction((parseResult, cancellationToken) =>
            DispatchAsync(new GetIssueByKey.Query(parseResult.GetValue(_key) ?? string.Empty), cancellationToken));
    }

    private async Task<int> DispatchAsync(GetIssueByKey.Query query, CancellationToken cancellationToken)
    {
        using var scope = _host.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetIssueByKey.Query, IssueResponse>>();

        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToConsole(IssuePresenter.RenderDetail);
    }
}
