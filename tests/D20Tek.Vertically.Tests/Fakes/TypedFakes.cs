namespace D20Tek.Vertically.Tests.Fakes;

/// <summary>
/// Feature that registers its handler with the typed, AOT-compatible overload. Used to verify
/// <c>AddFeature&lt;TFeature&gt;()</c>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TypedFeature : IFeature
{
    public void Register(IVerticallyBuilder builder) =>
        builder.Handlers.AddCommandHandler<Handler, Command, string>();

    public sealed record Command(string Value) : ICommand<string>;

    public sealed class Handler : ICommandHandler<Command, string>
    {
        public Task<Result<string>> HandleAsync(Command command, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<string>.Success(command.Value));
    }
}

/// <summary>A class that does not implement any pipeline behavior interface.</summary>
[ExcludeFromCodeCoverage]
public sealed class NotAClosedBehavior;
