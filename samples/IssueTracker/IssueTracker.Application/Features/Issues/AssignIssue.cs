using D20Tek.Functional;
using D20Tek.Functional.Async;
using D20Tek.Vertically;
using D20Tek.Vertically.Registration;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Application.Features.Issues;

/// <summary>
/// Vertical slice that assigns an <see cref="Issue"/> to a <see cref="User"/>. Validates the request
/// shape, verifies the issue and target user exist, and delegates the business rule (a closed issue
/// cannot be assigned) to the aggregate.
/// </summary>
public sealed class AssignIssue : IFeature
{
    public void Register(IVerticallyBuilder builder) =>
        builder.Handlers.AddCommandHandler<Handler>()
                        .AddValidator<Validator>();

    /// <summary>Request to assign <paramref name="IssueId"/> to <paramref name="AssigneeId"/>.</summary>
    public sealed record Command(Guid IssueId, Guid AssigneeId) : ICommand<IssueResponse>;

    /// <summary>Validates the assign-issue request before it reaches the handler.</summary>
    public sealed class Validator : IValidator<Command>
    {
        public ValidationErrors Validate(Command input)
        {
            var errors = ValidationErrors.Create();
            errors.AddIfError(() => input.IssueId == Guid.Empty, nameof(Command.IssueId), "IssueId is required.");
            errors.AddIfError(
                () => input.AssigneeId == Guid.Empty, nameof(Command.AssigneeId), "AssigneeId is required.");

            return errors;
        }
    }

    /// <summary>Loads the issue and user, enforces existence, and applies the assignment.</summary>
    public sealed class Handler(IIssueDbContext dbContext) : ICommandHandler<Command, IssueResponse>
    {
        private readonly IIssueDbContext _dbContext = dbContext;

        public Task<Result<IssueResponse>> HandleAsync(Command command, CancellationToken cancellationToken = default) =>
            _dbContext.FindIssueAsync(command.IssueId, cancellationToken: cancellationToken)
                .BindAsync(issue => EnsureUserExistsAsync(command.AssigneeId, issue, cancellationToken))
                .BindAsync(issue => Task.FromResult(issue.Assign(command.AssigneeId).Map(_ => issue)))
                .MapAsync(async issue =>
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return IssueResponse.FromIssue(issue);
                });

        private async Task<Result<Issue>> EnsureUserExistsAsync(Guid assigneeId, Issue issue, CancellationToken ct) =>
            await _dbContext.Users.AnyAsync(u => u.Id == assigneeId, ct)
                ? Result<Issue>.Success(issue)
                : Result<Issue>.Failure(Error.NotFound("user.notFound", $"User '{assigneeId}' was not found."));
    }
}
