using IssueTracker.Application.Domain;

namespace IssueTracker.Application.Features.Issues;

/// <summary>
/// Host-agnostic detail projection of an <see cref="Issue"/> returned by issue commands and queries.
/// </summary>
public sealed record IssueDetail(
    Guid Id,
    string Key,
    string Title,
    string? Description,
    IssueStatus Status,
    IssuePriority Priority,
    Guid? AssigneeId,
    DateTimeOffset CreatedUtc)
{
    public static IssueDetail FromIssue(Issue issue) => new(
        issue.Id,
        issue.Key,
        issue.Title,
        issue.Description,
        issue.Status,
        issue.Priority,
        issue.AssigneeId,
        issue.CreatedUtc);
}
