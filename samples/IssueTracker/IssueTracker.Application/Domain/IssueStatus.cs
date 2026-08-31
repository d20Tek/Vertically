namespace IssueTracker.Application.Domain;

/// <summary>
/// The lifecycle status of an <see cref="Issue"/>. Enum values intentionally match the primary keys
/// of the seeded <c>IssueStatuses</c> lookup table so the domain can reason in strongly-typed values
/// while persistence stores a foreign key.
/// </summary>
public enum IssueStatus
{
    Open = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4,
}
