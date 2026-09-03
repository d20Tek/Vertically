using D20Tek.Functional;
using D20Tek.Functional.Async;
using D20Tek.Vertically;
using D20Tek.Vertically.Registration;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Persistence;

namespace IssueTracker.Application.Features.Issues;

/// <summary>
/// Vertical slice that changes an <see cref="Issue"/>'s <see cref="IssuePriority"/>. Validates the
/// request shape, verifies the issue exists, and delegates the change to the aggregate.
/// </summary>
public sealed class ChangeIssuePriority : IFeature
{
    public void Register(IVerticallyBuilder builder) =>
        builder.Handlers.AddCommandHandler<Handler>()
                        .AddValidator<Validator>();

    /// <summary>Request to set <paramref name="IssueId"/>'s priority to <paramref name="Priority"/>.</summary>
    public sealed record Command(Guid IssueId, IssuePriority Priority) : ICommand<IssueResponse>;

    /// <summary>Validates the change-priority request before it reaches the handler.</summary>
    public sealed class Validator : IValidator<Command>
    {
        public ValidationErrors Validate(Command input)
        {
            var errors = ValidationErrors.Create();
            errors.AddIfError(() => input.IssueId == Guid.Empty, nameof(Command.IssueId), "IssueId is required.");
            errors.AddIfError(
                () => !Enum.IsDefined(input.Priority), nameof(Command.Priority), "Priority is not a recognized value.");

            return errors;
        }
    }

    /// <summary>Loads the issue, enforces existence, and applies the priority change.</summary>
    public sealed class Handler(IIssueDbContext dbContext) : ICommandHandler<Command, IssueResponse>
    {
        private readonly IIssueDbContext _dbContext = dbContext;

        public Task<Result<IssueResponse>> HandleAsync(Command command, CancellationToken cancellationToken = default) =>
            _dbContext.FindIssueAsync(command.IssueId, cancellationToken: cancellationToken)
                .BindAsync(issue => Task.FromResult(issue.Reprioritize(command.Priority).Map(_ => issue)))
                .MapAsync(async issue =>
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return IssueResponse.FromIssue(issue);
                });
    }
}
