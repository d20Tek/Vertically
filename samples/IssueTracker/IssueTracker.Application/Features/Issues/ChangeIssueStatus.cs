using D20Tek.Functional;
using D20Tek.Functional.Async;
using D20Tek.Vertically;
using D20Tek.Vertically.Registration;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace IssueTracker.Application.Features.Issues;

/// <summary>
/// Vertical slice that transitions an <see cref="Issue"/> to a new <see cref="IssueStatus"/>. Validates
/// the request shape, verifies the issue exists, and delegates the legal-transition rule to the aggregate.
/// </summary>
public sealed class ChangeIssueStatus : IFeature
{
    public void Register(IVerticallyBuilder builder) =>
        builder.Handlers.AddCommandHandler<Handler>()
                        .AddValidator<Validator>();

    /// <summary>Request to transition <paramref name="IssueId"/> to <paramref name="Status"/>.</summary>
    public sealed record Command(Guid IssueId, IssueStatus Status) : ICommand<IssueResponse>;

    /// <summary>Validates the change-status request before it reaches the handler.</summary>
    public sealed class Validator : IValidator<Command>
    {
        public ValidationErrors Validate(Command input)
        {
            var errors = ValidationErrors.Create();
            errors.AddIfError(() => input.IssueId == Guid.Empty, nameof(Command.IssueId), "IssueId is required.");
            errors.AddIfError(
                () => !Enum.IsDefined(input.Status), nameof(Command.Status), "Status is not a recognized value.");

            return errors;
        }
    }

    /// <summary>Loads the issue, enforces existence, and applies the status transition.</summary>
    public sealed class Handler(IIssueDbContext dbContext) : ICommandHandler<Command, IssueResponse>
    {
        private readonly IIssueDbContext _dbContext = dbContext;

        public Task<Result<IssueResponse>> HandleAsync(Command command, CancellationToken cancellationToken = default) =>
            FindIssueAsync(command.IssueId, cancellationToken)
                .BindAsync(issue => Task.FromResult(issue.ChangeStatus(command.Status).Map(_ => issue)))
                .MapAsync(async issue =>
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return IssueResponse.FromIssue(issue);
                });

        private async Task<Result<Issue>> FindIssueAsync(Guid issueId, CancellationToken ct)
        {
            var issue = await _dbContext.Issues.FirstOrDefaultAsync(i => i.Id == issueId, ct);
            return issue is null
                ? Result<Issue>.Failure(Error.NotFound("issue.notFound", $"Issue '{issueId}' was not found."))
                : Result<Issue>.Success(issue);
        }
    }
}
