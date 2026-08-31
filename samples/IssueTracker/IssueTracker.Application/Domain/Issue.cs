using D20Tek.Functional;

namespace IssueTracker.Application.Domain;

/// <summary>
/// The Issue aggregate. Encapsulates its own invariants: construction, assignment, and status
/// transitions flow through behavior methods that return <see cref="Result"/> so callers observe
/// business-rule failures instead of invalid state.
/// </summary>
public sealed class Issue
{
    private Issue() { }

    public Guid Id { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public IssueStatus Status { get; private set; }

    public IssuePriority Priority { get; private set; }

    public Guid? AssigneeId { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset UpdatedUtc { get; private set; }

    public static Issue Create(string key, string title, string? description, IssuePriority priority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var now = DateTimeOffset.UtcNow;
        return new Issue
        {
            Id = Guid.CreateVersion7(),
            Key = key.Trim(),
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Status = IssueStatus.Open,
            Priority = priority,
            AssigneeId = null,
            CreatedUtc = now,
            UpdatedUtc = now,
        };
    }

    public Result<Unit> Assign(Guid userId)
    {
        if (Status == IssueStatus.Closed)
        {
            return Result.Failure(
                Error.Conflict("issue.assign.closed", "A closed issue cannot be assigned."));
        }

        AssigneeId = userId;
        Touch();
        return Result.Success();
    }

    public Result<Unit> Unassign()
    {
        AssigneeId = null;
        Touch();
        return Result.Success();
    }

    public Result<Unit> ChangeStatus(IssueStatus target)
    {
        if (target == Status)
        {
            Touch();
            return Result.Success();
        }

        if (!IsLegalTransition(Status, target))
        {
            return Result.Failure(Error.Conflict(
                "issue.status.illegalTransition",
                $"Cannot change status from {Status} to {target}."));
        }

        Status = target;
        Touch();
        return Result.Success();
    }

    public Result<Unit> Rename(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title.Trim();
        Touch();
        return Result.Success();
    }

    public Result<Unit> Reprioritize(IssuePriority priority)
    {
        Priority = priority;
        Touch();
        return Result.Success();
    }

    private static bool IsLegalTransition(IssueStatus from, IssueStatus to) =>
        (from, to) switch
        {
            (IssueStatus.Open, IssueStatus.InProgress) => true,
            (IssueStatus.InProgress, IssueStatus.Resolved) => true,
            (IssueStatus.Resolved, IssueStatus.Closed) => true,
            (IssueStatus.Resolved, IssueStatus.InProgress) => true,
            _ => false,
        };

    private void Touch() => UpdatedUtc = DateTimeOffset.UtcNow;
}
