using D20Tek.Vertically;
using IssueTracker.Application.Features.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace IssueTracker.Cli.Commands.Users;

/// <summary>
/// The <c>user create</c> verb: dispatches <see cref="CreateUser"/> to add a new user — the CLI is the
/// only way to add users (an admin-style tool). Pipeline validation and the unique-email conflict are
/// surfaced to stderr with a non-zero exit code by the shared console translation helper; the new user's
/// id is printed on success so it can be used with <c>issue assign</c>. Each invocation resolves the
/// handler from a fresh DI scope so it gets its own <c>IIssueDbContext</c>.
/// </summary>
internal sealed class UserCreateCommand : Command
{
    private readonly IHost _host;

    private readonly Option<string> _firstName = new("--first-name")
    {
        Description = "The user's first name.",
        Required = true,
    };

    private readonly Option<string> _lastName = new("--last-name")
    {
        Description = "The user's last name.",
        Required = true,
    };

    private readonly Option<string> _email = new("--email")
    {
        Description = "The user's email address (must be unique).",
        Required = true,
    };

    public UserCreateCommand(IHost host) : base("create", "Create a new user.")
    {
        _host = host;
        Add(_firstName);
        Add(_lastName);
        Add(_email);
        SetAction((parseResult, cancellationToken) => DispatchAsync(
            new CreateUser.Command(
                parseResult.GetValue(_firstName) ?? string.Empty,
                parseResult.GetValue(_lastName) ?? string.Empty,
                parseResult.GetValue(_email) ?? string.Empty),
            cancellationToken));
    }

    private async Task<int> DispatchAsync(CreateUser.Command command, CancellationToken cancellationToken)
    {
        using var scope = _host.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateUser.Command, UserResponse>>();

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToConsole(user => $"Created user {user.Id} ({user.FullName} <{user.Email}>).");
    }
}
