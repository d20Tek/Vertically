using D20Tek.Vertically;
using IssueTracker.Application.Features.Users;
using IssueTracker.Cli.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace IssueTracker.Cli.Commands.Users;

/// <summary>
/// The <c>user list</c> verb: dispatches <see cref="GetUsers"/> and renders the users as a plain-text
/// table (with their full ids) via <see cref="UserPresenter"/>. It exists so CLI operators can look up
/// the user id to pass to <c>issue assign &lt;key&gt; --user &lt;id&gt;</c>. Each invocation resolves the
/// handler from a fresh DI scope so it gets its own <c>IIssueDbContext</c>.
/// </summary>
internal sealed class UserListCommand : Command
{
    private readonly IHost _host;

    public UserListCommand(IHost host) : base("list", "List users (with their ids for issue assignment).")
    {
        _host = host;
        SetAction((_, cancellationToken) => DispatchAsync(cancellationToken));
    }

    private async Task<int> DispatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _host.Services.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<IQueryHandler<GetUsers.Query, IReadOnlyList<UserResponse>>>();

        var result = await handler.HandleAsync(new GetUsers.Query(), cancellationToken);
        return result.ToConsole(UserPresenter.RenderList);
    }
}
