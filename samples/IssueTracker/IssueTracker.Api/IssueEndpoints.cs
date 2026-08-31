using D20Tek.Vertically;
using D20Tek.Vertically.Queries.Pagination;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Features.Issues;

namespace IssueTracker.Api;

/// <summary>
/// Maps the issue vertical slices to minimal-API endpoints, resolving each slice's handler from DI
/// and translating its <c>Result&lt;T&gt;</c> to HTTP via <see cref="ResultHttpExtensions"/>.
/// </summary>
internal static class IssueEndpoints
{
    public static IEndpointRouteBuilder MapIssueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/issues").WithTags("Issues");

        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapGet("/", GetPageAsync);
        group.MapPost("/{id:guid}/assign", AssignAsync);
        group.MapPost("/{id:guid}/status", ChangeStatusAsync);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateIssueRequest request,
        ICommandHandler<CreateIssue.Command, IssueResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateIssue.Command(request.Title, request.Description, request.Priority, request.Key);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToCreated(issue => $"/issues/{issue.Id}");
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IQueryHandler<GetIssueById.Query, IssueResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetIssueById.Query(id), cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> GetPageAsync(
        HttpRequest httpRequest,
        IQueryHandler<GetIssues.Query, PageOf<IssueResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = IssueQueryBinder.Bind(httpRequest.Query);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> AssignAsync(
        Guid id,
        AssignIssueRequest request,
        ICommandHandler<AssignIssue.Command, IssueResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AssignIssue.Command(id, request.AssigneeId), cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid id,
        ChangeIssueStatusRequest request,
        ICommandHandler<ChangeIssueStatus.Command, IssueResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ChangeIssueStatus.Command(id, request.Status), cancellationToken);
        return result.ToOk();
    }
}

/// <summary>Body for <c>POST /issues</c>.</summary>
internal sealed record CreateIssueRequest(string Title, string? Description, IssuePriority Priority, string? Key = null);

/// <summary>Body for <c>POST /issues/{id}/assign</c>.</summary>
internal sealed record AssignIssueRequest(Guid AssigneeId);

/// <summary>Body for <c>POST /issues/{id}/status</c>.</summary>
internal sealed record ChangeIssueStatusRequest(IssueStatus Status);
