namespace IssueTracker.Application.Domain;

/// <summary>
/// The priority of an <see cref="Issue"/>. Enum values intentionally match the primary keys of the
/// seeded <c>IssuePriorities</c> lookup table so the domain can reason in strongly-typed values while
/// persistence stores a foreign key.
/// </summary>
public enum IssuePriority
{
    Critical = 1,
    High = 2,
    Medium = 3,
    Low = 4,
}
