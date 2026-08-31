namespace IssueTracker.Application.Domain;

/// <summary>
/// A person who can be assigned to an <see cref="Issue"/>. Referenced by <see cref="Issue.AssigneeId"/>
/// and retrievable for UI assignee selectors.
/// </summary>
public sealed class User
{
    private User() { }

    public Guid Id { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset UpdatedUtc { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    public static User Create(string firstName, string lastName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var now = DateTimeOffset.UtcNow;
        return new User
        {
            Id = Guid.CreateVersion7(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim(),
            CreatedUtc = now,
            UpdatedUtc = now,
        };
    }
}
