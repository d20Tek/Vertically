using D20Tek.Functional;
using D20Tek.Functional.Async;
using D20Tek.Vertically;
using D20Tek.Vertically.Registration;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Application.Features.Users;

/// <summary>
/// Vertical slice that creates a new <see cref="User"/>. Groups its command, validator, and handler into
/// a single self-registering unit. Input is validated by <see cref="Validator"/> before the handler runs,
/// and the handler enforces the unique-email rule so the domain factory can assume valid input.
/// </summary>
public sealed class CreateUser : IFeature
{
    public const int MaxNameLength = 100;

    public const int MaxEmailLength = 256;

    public void Register(IVerticallyBuilder builder) =>
        builder.Handlers.AddCommandHandler<Handler>()
                        .AddValidator<Validator>();

    /// <summary>Request to create a new user.</summary>
    public sealed record Command(string FirstName, string LastName, string Email) : ICommand<UserResponse>;

    /// <summary>Validates the create-user request before it reaches the handler.</summary>
    public sealed class Validator : IValidator<Command>
    {
        public ValidationErrors Validate(Command input)
        {
            var errors = ValidationErrors.Create();
            errors.AddIfError(
                () => string.IsNullOrWhiteSpace(input.FirstName), nameof(Command.FirstName), "FirstName is required.");
            errors.AddIfError(
                () => input.FirstName?.Length > MaxNameLength,
                nameof(Command.FirstName),
                $"FirstName must not exceed {MaxNameLength} characters.");
            errors.AddIfError(
                () => string.IsNullOrWhiteSpace(input.LastName), nameof(Command.LastName), "LastName is required.");
            errors.AddIfError(
                () => input.LastName?.Length > MaxNameLength,
                nameof(Command.LastName),
                $"LastName must not exceed {MaxNameLength} characters.");
            errors.AddIfError(() => string.IsNullOrWhiteSpace(input.Email), nameof(Command.Email), "Email is required.");
            errors.AddIfError(
                () => input.Email?.Length > MaxEmailLength,
                nameof(Command.Email),
                $"Email must not exceed {MaxEmailLength} characters.");
            errors.AddIfError(
                () => !string.IsNullOrWhiteSpace(input.Email) && !input.Email.Contains('@'),
                nameof(Command.Email),
                "Email must be a valid email address.");

            return errors;
        }
    }

    /// <summary>Enforces the unique-email rule, persists the new user, and returns its summary.</summary>
    public sealed class Handler(IIssueDbContext dbContext) : ICommandHandler<Command, UserResponse>
    {
        private readonly IIssueDbContext _dbContext = dbContext;

        public Task<Result<UserResponse>> HandleAsync(Command command, CancellationToken cancellationToken = default) =>
            EnsureEmailIsUniqueAsync(command.Email.Trim(), cancellationToken)
                .MapAsync(_ => Task.FromResult(CreateAndAddUser(command)))
                .MapAsync(async user =>
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return UserResponse.FromUser(user);
                });

        private async Task<Result<Unit>> EnsureEmailIsUniqueAsync(string email, CancellationToken ct) =>
            await _dbContext.Users.AnyAsync(u => u.Email == email, ct)
                ? Result<Unit>.Failure(Error.Conflict("user.email.duplicate", $"A user with email '{email}' already exists."))
                : Result<Unit>.Success(Unit.Value);

        private User CreateAndAddUser(Command command)
        {
            var user = User.Create(command.FirstName, command.LastName, command.Email);
            _dbContext.Users.Add(user);
            return user;
        }
    }
}
