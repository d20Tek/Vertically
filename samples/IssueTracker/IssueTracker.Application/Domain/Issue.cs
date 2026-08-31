using D20Tek.Functional;

namespace IssueTracker.Application.Domain;

/// <summary>
/// The Issue aggregate. Encapsulates its own invariants: construction, assignment, and status
/// transitions flow through behavior methods that return <see cref="Result"/> so callers observe
/// business-rule failures instead of invalid state.
/// </summary>
public sealed class Issue
{
    internal Issue(Guid id, string key, string title, string? description, IssueStatus status, IssuePriority priority,
                   Guid? assigneeId, DateTimeOffset createdUtc, DateTimeOffset updatedUtc)
    {
        Id = id;
        Key = key;
        Title = title;
        Description = description;
        Status = status;
        Priority = priority;
        AssigneeId = assigneeId;
        CreatedUtc = createdUtc;
        UpdatedUtc = updatedUtc;
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public IssueStatus Status { get; private set; }

    public IssuePriority Priority { get; private set; }

    public Guid? AssigneeId { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset UpdatedUtc { get; private set; }

    public Result<Unit> Assign(Guid userId)
    {
        if (Status == IssueStatus.Closed)
        {
            return Result.Failure(
                Error.Conflict("issue.assign.closed", "A closed issue cannot be assigned."));
        }

        AssigneeId = userId;
        return Touch();
    }

    public Result<Unit> Unassign()
    {
        AssigneeId = null;
        return Touch();
    }

    public Result<Unit> ChangeStatus(IssueStatus target)
    {
        if (target == Status) return Touch();

        return IssueStatusPolicy.EnsureCanTransition(Status, target).Bind(_ =>
            {
                Status = target;
                return Touch();
            });
    }

    public Result<Unit> Rename(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title.Trim();
        return Touch();
    }

    public Result<Unit> Reprioritize(IssuePriority priority)
    {
        Priority = priority;
        return Touch();
    }

    private Result<Unit> Touch()
    {
        UpdatedUtc = DateTimeOffset.UtcNow;
        return Result.Success();
    }
}