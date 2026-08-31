using IssueTracker.Application.Domain;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Application.Persistence;

/// <summary>
/// Abstraction over the Issue Tracker data store that command/query handlers depend on directly.
/// Exposing <see cref="DbSet{TEntity}"/> keeps querying server-side (efficient paging + async) while
/// letting the concrete <c>IssueDbContext</c> and its provider live in the persistence project.
/// </summary>
public interface IIssueDbContext
{
    DbSet<Issue> Issues { get; }

    DbSet<User> Users { get; }

    DbSet<IssueStatusRef> IssueStatuses { get; }

    DbSet<IssuePriorityRef> IssuePriorities { get; }

    /// <summary>
    /// Atomically reserves and returns the next monotonic number used to build a friendly issue
    /// <c>Key</c> (e.g. <c>ISSUE-{n}</c>). Backed by a persistent counter so keys are collision-free
    /// without a scan-and-retry loop.
    /// </summary>
    Task<long> NextIssueKeyNumberAsync(CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
