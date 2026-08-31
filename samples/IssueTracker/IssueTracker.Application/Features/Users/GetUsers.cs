using D20Tek.Functional;
using D20Tek.Vertically;
using D20Tek.Vertically.Registration;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Application.Features.Users;

/// <summary>
/// Vertical slice that returns the list of <see cref="User"/>s (as <see cref="UserResponse"/>) for UI
/// assignee selectors, ordered by name.
/// </summary>
public sealed class GetUsers : IFeature
{
    private const int _hardUserLimit = 100;

    public void Register(IVerticallyBuilder builder) =>
        builder.Handlers.AddQueryHandler<Handler>();

    /// <summary>Request for all users available as assignees.</summary>
    public sealed record Query : IQuery<IReadOnlyList<UserResponse>>;

    /// <summary>Loads users (read-only), ordered by name, and projects them to summaries.</summary>
    public sealed class Handler(IIssueDbContext dbContext) : IQueryHandler<Query, IReadOnlyList<UserResponse>>
    {
        private readonly IIssueDbContext _dbContext = dbContext;

        public async Task<Result<IReadOnlyList<UserResponse>>> HandleAsync(
            Query query, CancellationToken cancellationToken = default)
        {
            var users = await _dbContext.Users
                .AsNoTracking()
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Take(_hardUserLimit)
                .Select(u => new UserResponse(u.Id, u.FullName, u.Email))
                .ToListAsync(cancellationToken);

            return Result<IReadOnlyList<UserResponse>>.Success(users);
        }
    }
}
