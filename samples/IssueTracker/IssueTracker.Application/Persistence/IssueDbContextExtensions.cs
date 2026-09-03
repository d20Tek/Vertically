using D20Tek.Functional;
using IssueTracker.Application.Domain;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Application.Persistence;

/// <summary>
/// Shared query helpers over <see cref="IIssueDbContext"/>. Loading an aggregate by id (or returning a
/// consistent not-found <see cref="Result"/>) is a generic persistence concern rather than slice-specific
/// business logic, so centralizing it keeps each handler focused on its own intent while guaranteeing a
/// uniform error shape.
/// </summary>
internal static class IssueDbContextExtensions
{
    /// <summary>
    /// Loads the <see cref="Issue"/> identified by <paramref name="issueId"/>, returning a not-found
    /// failure when it does not exist. Pass <paramref name="asNoTracking"/> for read-only queries so
    /// EF Core skips change tracking.
    /// </summary>
    internal static async Task<Result<Issue>> FindIssueAsync(
        this IIssueDbContext dbContext,
        Guid issueId,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = asNoTracking ? dbContext.Issues.AsNoTracking() : dbContext.Issues;
        var issue = await query.FirstOrDefaultAsync(i => i.Id == issueId, cancellationToken);

        return issue is null
            ? Result<Issue>.Failure(Error.NotFound("issue.notFound", $"Issue '{issueId}' was not found."))
            : Result<Issue>.Success(issue);
    }

    /// <summary>
    /// Loads the <see cref="Issue"/> identified by its friendly <paramref name="key"/> (e.g. <c>ISSUE-1</c>),
    /// returning a not-found failure when it does not exist. The lookup is case-insensitive. Pass
    /// <paramref name="asNoTracking"/> for read-only queries so EF Core skips change tracking.
    /// </summary>
    internal static async Task<Result<Issue>> FindIssueByKeyAsync(
        this IIssueDbContext dbContext,
        string key,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = (key ?? string.Empty).Trim().ToUpperInvariant();
        var query = asNoTracking ? dbContext.Issues.AsNoTracking() : dbContext.Issues;
        var issue = await query.FirstOrDefaultAsync(i => i.Key == normalizedKey, cancellationToken);

        return issue is null
            ? Result<Issue>.Failure(Error.NotFound("issue.notFound", $"Issue '{key}' was not found."))
            : Result<Issue>.Success(issue);
    }
}
