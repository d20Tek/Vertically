using IssueTracker.Application.Domain;

namespace IssueTracker.Web.Components;

/// <summary>
/// Shared helpers that map issue status/priority enum values to the scoped badge CSS modifier
/// classes used across the board and detail pages.
/// </summary>
public static class IssueBadges
{
    /// <summary>Maps an <see cref="IssueStatus"/> to its badge modifier class.</summary>
    public static string StatusClass(IssueStatus status) => status switch
    {
        IssueStatus.Open => "is-open",
        IssueStatus.InProgress => "is-inprogress",
        IssueStatus.Resolved => "is-resolved",
        IssueStatus.Closed => "is-closed",
        _ => string.Empty,
    };

    /// <summary>Maps an <see cref="IssuePriority"/> to its badge modifier class.</summary>
    public static string PriorityClass(IssuePriority priority) => priority switch
    {
        IssuePriority.Low => "is-low",
        IssuePriority.Medium => "is-medium",
        IssuePriority.High => "is-high",
        IssuePriority.Critical => "is-critical",
        _ => string.Empty,
    };
}
