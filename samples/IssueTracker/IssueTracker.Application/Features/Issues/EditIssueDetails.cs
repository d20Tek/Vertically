using D20Tek.Functional;
using D20Tek.Functional.Async;
using D20Tek.Vertically;
using D20Tek.Vertically.Registration;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Persistence;

namespace IssueTracker.Application.Features.Issues;

/// <summary>
/// Vertical slice that edits an <see cref="Issue"/>'s title and description. Validates the request
/// shape (reusing <see cref="CreateIssue"/>'s length rules), verifies the issue exists, and delegates
/// the changes to the aggregate via <see cref="Issue.Rename"/> and <see cref="Issue.UpdateDescription"/>.
/// </summary>
public sealed class EditIssueDetails : IFeature
{
    public void Register(IVerticallyBuilder builder) =>
        builder.Handlers.AddCommandHandler<Handler>()
                        .AddValidator<Validator>();

    /// <summary>Request to update <paramref name="IssueId"/>'s title and description.</summary>
    public sealed record Command(Guid IssueId, string Title, string? Description) : ICommand<IssueResponse>;

    /// <summary>Validates the edit-details request before it reaches the handler.</summary>
    public sealed class Validator : IValidator<Command>
    {
        public ValidationErrors Validate(Command input)
        {
            var errors = ValidationErrors.Create();
            errors.AddIfError(() => input.IssueId == Guid.Empty, nameof(Command.IssueId), "IssueId is required.");
            errors.AddIfError(() => string.IsNullOrWhiteSpace(input.Title), nameof(Command.Title), "Title is required.");
            errors.AddIfError(
                () => input.Title?.Length > CreateIssue.MaxTitleLength,
                nameof(Command.Title),
                $"Title must not exceed {CreateIssue.MaxTitleLength} characters.");
            errors.AddIfError(
                () => input.Description?.Length > CreateIssue.MaxDescriptionLength,
                nameof(Command.Description),
                $"Description must not exceed {CreateIssue.MaxDescriptionLength} characters.");

            return errors;
        }
    }

    /// <summary>Loads the issue, enforces existence, and applies the title/description changes.</summary>
    public sealed class Handler(IIssueDbContext dbContext) : ICommandHandler<Command, IssueResponse>
    {
        private readonly IIssueDbContext _dbContext = dbContext;

        public Task<Result<IssueResponse>> HandleAsync(Command command, CancellationToken cancellationToken = default) =>
            _dbContext.FindIssueAsync(command.IssueId, cancellationToken: cancellationToken)
                .BindAsync(issue => Task.FromResult(UpdateIssueDetails(issue, command)))
                .MapAsync(async issue =>
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return IssueResponse.FromIssue(issue);
                });

        private static Result<Issue> UpdateIssueDetails(Issue issue, Command command) =>
            issue.Rename(command.Title)
                 .Bind(_ => issue.UpdateDescription(command.Description))
                 .Map(_ => issue);

    }
}
