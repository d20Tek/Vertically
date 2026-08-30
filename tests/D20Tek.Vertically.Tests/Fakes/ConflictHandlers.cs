namespace D20Tek.Vertically.Tests.Fakes;

/// <summary>Command used exclusively for handler dedupe/conflict tests.</summary>
[ExcludeFromCodeCoverage]
public sealed record ConflictCommand(string Value) : ICommand<string>;

/// <summary>
/// Marker used to close the open-generic conflict handlers for explicit registration.
/// The handlers are open generic on purpose so whole-assembly scanning
/// (<see cref="D20Tek.Vertically.Registration.HandlerTypeInspector.IsConcreteClass"/> excludes
/// open generics) never discovers them and reports a false conflict against
/// <see cref="ConflictCommand"/>.
/// </summary>
public sealed class ScanExcluded;

/// <summary>First handler for <see cref="ConflictCommand"/>. Open generic to avoid scan discovery.</summary>
[ExcludeFromCodeCoverage]
public sealed class ConflictHandlerA<TMarker> : ICommandHandler<ConflictCommand, string>
{
    public Task<Result<string>> HandleAsync(ConflictCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<string>.Success(command.Value));
}

/// <summary>Second, different handler for the same <see cref="ConflictCommand"/>. Open generic to avoid scan discovery.</summary>
[ExcludeFromCodeCoverage]
public sealed class ConflictHandlerB<TMarker> : ICommandHandler<ConflictCommand, string>
{
    public Task<Result<string>> HandleAsync(ConflictCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<string>.Success(command.Value));
}
