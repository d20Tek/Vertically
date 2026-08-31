using D20Tek.Functional;
using D20Tek.Vertically;
using D20Tek.Vertically.Registration;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Application.Features.Issues;

/// <summary>
/// Vertical slice that retrieves a single <see cref="Issue"/> by its identifier, returning an
/// <see cref="IssueResponse"/> or a not-found <see cref="Result"/>.
/// </summary>
public sealed class GetIssueById : IFeature
{
    public void Register(IVerticallyBuilder builder) =>
        builder.Handlers.AddQueryHandler<Handler>()
                        .AddValidator<Validator>();

    /// <summary>Request for the issue identified by <paramref name="IssueId"/>.</summary>
    public sealed record Query(Guid IssueId) : IQuery<IssueResponse>;

    /// <summary>Validates the get-issue request before it reaches the handler.</summary>
    public sealed class Validator : IValidator<Query>
    {
        public ValidationErrors Validate(Query input)
        {
            var errors = ValidationErrors.Create();
            errors.AddIfError(() => input.IssueId == Guid.Empty, nameof(Query.IssueId), "IssueId is required.");

            return errors;
        }
    }

    /// <summary>Loads the issue (read-only) and projects it to an <see cref="IssueResponse"/>.</summary>
    public sealed class Handler(IIssueDbContext dbContext) : IQueryHandler<Query, IssueResponse>
    {
        private readonly IIssueDbContext _dbContext = dbContext;

        public async Task<Result<IssueResponse>> HandleAsync(Query query, CancellationToken cancellationToken = default)
        {
            var issue = await _dbContext.Issues
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == query.IssueId, cancellationToken);

            return issue is null
                ? Result<IssueResponse>.Failure(Error.NotFound("issue.notFound", $"Issue '{query.IssueId}' was not found."))
                : Result<IssueResponse>.Success(IssueResponse.FromIssue(issue));
        }
    }
}
