using D20Tek.Functional;
using D20Tek.Functional.Async;
using D20Tek.Vertically;
using D20Tek.Vertically.Registration;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Application.Features.Issues;

/// <summary>
/// Vertical slice that creates a new <see cref="Issue"/>. Groups its command, validator, and handler
/// into a single self-registering unit. Input is validated by <see cref="Validator"/> before the
/// handler runs, so the domain factory can assume valid input.
/// </summary>
public sealed class CreateIssue : IFeature
{
    public const int MaxTitleLength = 200;

    public const int MaxDescriptionLength = 4000;

    public void Register(IVerticallyBuilder builder) =>
        builder.Handlers.AddCommandHandler<Handler>()
                        .AddValidator<Validator>();

    /// <summary>Request to create a new issue. When <see cref="Key"/> is null, one is auto-generated.</summary>
    public sealed record Command(string Title, string? Description, IssuePriority Priority, string? Key = null)
        : ICommand<IssueResponse>;

    /// <summary>Validates the create-issue request before it reaches the handler.</summary>
    public sealed class Validator : IValidator<Command>
    {
        public ValidationErrors Validate(Command input)
        {
            var errors = ValidationErrors.Create();
            errors.AddIfError(() => string.IsNullOrWhiteSpace(input.Title), nameof(Command.Title), "Title is required.");
            errors.AddIfError(
                () => input.Title?.Length > MaxTitleLength,
                nameof(Command.Title),
                $"Title must not exceed {MaxTitleLength} characters.");
            errors.AddIfError(
                () => input.Description?.Length > MaxDescriptionLength,
                nameof(Command.Description),
                $"Description must not exceed {MaxDescriptionLength} characters.");
            errors.AddIfError(
                () => !Enum.IsDefined(input.Priority), nameof(Command.Priority), "Priority is not a recognized value.");

            return errors;
        }
    }

    /// <summary>Persists the new issue via <see cref="IIssueDbContext"/> and returns its detail.</summary>
    public sealed class Handler(IIssueDbContext dbContext) : ICommandHandler<Command, IssueResponse>
    {
        private readonly IIssueDbContext _dbContext = dbContext;

        public Task<Result<IssueResponse>> HandleAsync(Command command, CancellationToken cancellationToken = default) =>
            ResolveKeyAsync(command, cancellationToken)
                .BindAsync(key => EnsureKeyIsUniqueAsync(key, cancellationToken))
                .MapAsync(key => Task.FromResult(CreateAndAddIssue(key, command)))
                .MapAsync(async issue =>
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return IssueResponse.FromIssue(issue);
                });

        private async Task<Result<string>> ResolveKeyAsync(Command command, CancellationToken ct) =>
            Result<string>.Success(
                string.IsNullOrWhiteSpace(command.Key) ? await GenerateKeyAsync(ct) : command.Key.Trim());

        private async Task<Result<string>> EnsureKeyIsUniqueAsync(string key, CancellationToken ct) =>
            await _dbContext.Issues.AnyAsync(i => i.Key == key, ct)
                ? Result<string>.Failure(Error.Conflict("issue.key.duplicate", $"An issue with key '{key}' already exists."))
                : Result<string>.Success(key);

        private Issue CreateAndAddIssue(string key, Command command)
        {
            var issue = Issue.Create(key, command.Title, command.Description, command.Priority);
            _dbContext.Issues.Add(issue);
            return issue;
        }

        private async Task<string> GenerateKeyAsync(CancellationToken cancellationToken) =>
            $"ISSUE-{await _dbContext.NextIssueKeyNumberAsync(cancellationToken)}";
    }
}
