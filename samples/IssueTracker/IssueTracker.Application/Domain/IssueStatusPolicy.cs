using D20Tek.Functional;

namespace IssueTracker.Application.Domain;

internal static class IssueStatusPolicy
{
    public static bool CanTransition(IssueStatus from, IssueStatus to) =>
        (from, to) switch
        {
            (IssueStatus.Open, IssueStatus.InProgress) => true,
            (IssueStatus.InProgress, IssueStatus.Resolved) => true,
            (IssueStatus.Resolved, IssueStatus.Closed) => true,
            (IssueStatus.Resolved, IssueStatus.InProgress) => true,
            _ => false,
        };

    public static Result<Unit> EnsureCanTransition(IssueStatus from, IssueStatus to) =>
        CanTransition(from, to)
            ? Result.Success()
            : Result.Failure(Error.Conflict(
                "issue.status.illegalTransition",
                $"Cannot change status from {from} to {to}."));
}
