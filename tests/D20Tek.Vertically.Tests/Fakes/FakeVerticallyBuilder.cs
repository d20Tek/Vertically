namespace D20Tek.Vertically.Tests.Fakes;

/// <summary>
/// Minimal <see cref="IVerticallyBuilder"/> stand-in that is not the internal
/// <c>VerticallyBuilder</c> implementation, used to verify that diagnostics helpers requiring the
/// concrete builder type reject other implementations.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class FakeVerticallyBuilder : IVerticallyBuilder
{
    public IServiceCollection Services => throw new NotSupportedException();

    public IHandlerRegistrationBuilder Handlers => throw new NotSupportedException();

    public IBehaviorRegistrationBuilder Behaviors => throw new NotSupportedException();

    public IHandlerBehaviorScope ForCommand<TCommand>() => throw new NotSupportedException();

    public IHandlerBehaviorScope ForQuery<TQuery>() => throw new NotSupportedException();

    public IReadOnlyList<HandlerRegistrationInfo> HandlerRegistrations => [];

    public IReadOnlyList<ValidatorRegistrationInfo> ValidatorRegistrations => [];
}
