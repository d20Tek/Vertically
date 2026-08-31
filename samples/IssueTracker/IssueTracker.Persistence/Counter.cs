namespace IssueTracker.Persistence;

/// <summary>
/// Persistent named counter row. Used to generate collision-free, monotonic numbers (e.g. the issue
/// key sequence) without relying on a database sequence, which the SQLite provider does not support.
/// </summary>
internal sealed class Counter
{
    /// <summary>The counter's unique name (primary key).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The last value handed out for this counter.</summary>
    public long Value { get; set; }
}
