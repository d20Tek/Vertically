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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
