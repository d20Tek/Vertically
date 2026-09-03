using IssueTracker.Application.Domain;

namespace IssueTracker.Api;

/// <summary>Body for <c>POST /issues</c>.</summary>
internal sealed record CreateIssueRequest(string Title, string? Description, IssuePriority Priority, string? Key = null);

/// <summary>Body for <c>POST /issues/{id}/assign</c>.</summary>
internal sealed record AssignIssueRequest(Guid AssigneeId);

/// <summary>Body for <c>POST /issues/{id}/status</c>.</summary>
internal sealed record ChangeIssueStatusRequest(IssueStatus Status);
