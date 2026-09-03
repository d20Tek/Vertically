using D20Tek.Functional;
using D20Tek.Functional.Async;
using D20Tek.Vertically;
using D20Tek.Vertically.Registration;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Persistence;

namespace IssueTracker.Application.Features.Issues;

/// <summary>
/// Vertical slice that retrieves a single <see cref="Issue"/> by its friendly <c>Key</c> (e.g.
/// <c>ISSUE-1</c>), returning an <see cref="IssueResponse"/> or a not-found <see cref="Result"/>. This
/// supports host surfaces (such as the CLI) that identify issues by their human-readable key rather than
/// their Guid.
/// </summary>
public sealed class GetIssueByKey : IFeature
{
    public void Register(IVerticallyBuilder builder) =>
        builder.Handlers.AddQueryHandler<Handler>()
                        .AddValidator<Validator>();

    /// <summary>Request for the issue identified by <paramref name="Key"/>.</summary>
    public sealed record Query(string Key) : IQuery<IssueResponse>;

    /// <summary>Validates the get-issue-by-key request before it reaches the handler.</summary>
    public sealed class Validator : IValidator<Query>
    {
        public ValidationErrors Validate(Query input)
        {
            var errors = ValidationErrors.Create();
            errors.AddIfError(() => string.IsNullOrWhiteSpace(input.Key), nameof(Query.Key), "Key is required.");

            return errors;
        }
    }

    /// <summary>Loads the issue (read-only) by key and projects it to an <see cref="IssueResponse"/>.</summary>
    public sealed class Handler(IIssueDbContext dbContext) : IQueryHandler<Query, IssueResponse>
    {
        private readonly IIssueDbContext _dbContext = dbContext;

        public Task<Result<IssueResponse>> HandleAsync(Query query, CancellationToken cancellationToken = default) =>
            _dbContext.FindIssueByKeyAsync(query.Key, asNoTracking: true, cancellationToken)
                .MapAsync(issue => Task.FromResult(IssueResponse.FromIssue(issue)));
    }
}
