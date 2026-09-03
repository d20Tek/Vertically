using IssueTracker.Application.Features.Users;

namespace IssueTracker.Cli.Rendering;

/// <summary>
/// Presentation helper that renders <see cref="UserResponse"/> data as a plain-text table for the
/// <c>user list</c> verb. The full <c>Id</c> is shown (not truncated) because it is needed as the
/// <c>--user</c> argument for <c>issue assign &lt;key&gt; --user &lt;id&gt;</c>.
/// </summary>
internal static class UserPresenter
{
    /// <summary>Renders the list of users as a plain-text table with their full ids.</summary>
    public static string RenderList(IReadOnlyList<UserResponse> users)
    {
        var headers = new[] { "Id", "Name", "Email" };
        var rows = users
            .Select(u => (IReadOnlyList<string>)
            [
                u.Id.ToString(),
                u.FullName,
                u.Email,
            ])
            .ToList();

        return ConsoleFormatter.Table(headers, rows, "No users found.");
    }
}
