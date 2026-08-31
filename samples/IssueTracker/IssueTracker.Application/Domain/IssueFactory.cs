namespace IssueTracker.Application.Domain;

/// <summary>
/// Provides the <c>Issue.Create</c> factory as a static extension member on the <see cref="Issue"/>
/// type. Keeps construction/normalization out of the aggregate while still surfacing as
/// <c>Issue.Create(...)</c> at the call site. Input is expected to have been validated upstream by an
/// <c>IValidator</c>; the guards here are defensive and throw if that contract is violated.
/// </summary>
public static class IssueFactory
{
    extension(Issue)
    {
        /// <summary>
        /// Creates a new <see cref="Issue"/> in the <see cref="IssueStatus.Open"/> state, normalizing
        /// text fields and stamping timestamps.
        /// </summary>
        public static Issue Create(string key, string title, string? description, IssuePriority priority)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);

            var now = DateTimeOffset.UtcNow;
            return new Issue(
                id: Guid.CreateVersion7(),
                key: key.Trim(),
                title: title.Trim(),
                description: string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                status: IssueStatus.Open,
                priority: priority,
                assigneeId: null,
                createdUtc: now,
                updatedUtc: now);
        }
    }
}
