using D20Tek.Functional;
using D20Tek.Vertically;
using D20Tek.Vertically.Pipeline;
using D20Tek.Vertically.Registration;

namespace AotCli;

/// <summary>
/// Vertical slice that prints a test message. Registered with the typed, trim- and
/// native AOT-compatible overloads.
/// </summary>
public sealed class PrintMessage : IFeature
{
    public void Register(IVerticallyBuilder builder)
    {
        builder.Handlers.AddCommandHandler<Handler, Command, string>()
                        .AddValidator<Validator>();
        builder.ForCommand<Command>().Add<MarkerBehavior>();
    }

    public sealed record Command(string Message) : ICommand<string>;

    public sealed class Validator : IValidator<Command>
    {
        public ValidationErrors Validate(Command input)
        {
            var errors = ValidationErrors.Create();
            errors.AddIfError(() => string.IsNullOrWhiteSpace(input.Message), nameof(Command.Message), "A message is required.");
            return errors;
        }
    }

    public sealed class Handler : ICommandHandler<Command, string>
    {
        public Task<Result<string>> HandleAsync(Command command, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[handler] {command.Message}");
            return Task.FromResult(Result<string>.Success(command.Message));
        }
    }

    /// <summary>Closed custom behavior registered with Add&lt;TBehavior&gt;().</summary>
    public sealed class MarkerBehavior : IPipelineBehavior<Command, string>
    {
        public async Task<Result<string>> HandleAsync(
            Command request,
            RequestHandlerDelegate<string> next,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine("[behavior] before");
            var result = await next().ConfigureAwait(false);
            Console.WriteLine("[behavior] after");
            return result;
        }
    }
}
