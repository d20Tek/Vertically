using D20Tek.Vertically;
using IssueTracker.Application.Features.Users;

namespace IssueTracker.Api;

/// <summary>Maps the user vertical slices to minimal-API endpoints (assignee selector source).</summary>
internal static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/users", GetUsersAsync).WithTags("Users");
        return app;
    }

    private static async Task<IResult> GetUsersAsync(
        IQueryHandler<GetUsers.Query, IReadOnlyList<UserResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetUsers.Query(), cancellationToken);
        return result.ToOk();
    }
}
