namespace D20Tek.Vertically.Tests.Fakes;

/// <summary>
/// Self-registering feature used to verify feature-first discovery. Registers its own handler
/// and validator and configures a per-handler behavior.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SampleFeature : IFeature
{
    public void Register(IVerticallyBuilder builder)
    {
        builder.Handlers.AddCommandHandler<Handler>();
        builder.Handlers.AddValidator<Validator>();
        builder.ForCommand<Command>().AddTiming();
    }

    public sealed record Command(string Value) : ICommand<string>;

    public sealed class Validator : IValidator<Command>
    {
        public ValidationErrors Validate(Command input) => ValidationErrors.Create();
    }

    public sealed class Handler : ICommandHandler<Command, string>
    {
        public Task<Result<string>> HandleAsync(Command command, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<string>.Success(command.Value));
    }
}

