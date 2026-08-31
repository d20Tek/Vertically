namespace IssueTracker.Application.Domain;

/// <summary>
/// Reference-data row for the <c>IssueStatuses</c> lookup table. Its <see cref="Id"/> matches the
/// corresponding <see cref="IssueStatus"/> enum value, and it carries a display name and sort order
/// for UI selectors.
/// </summary>
public sealed class IssueStatusRef
{
    public IssueStatus Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
