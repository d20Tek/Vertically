namespace D20Tek.Vertically.Registration;

using D20Tek.Vertically.Behaviors;

internal sealed class HandlerBehaviorScope(VerticallyBuilder builder, Type requestType) : IHandlerBehaviorScope
{
    private readonly VerticallyBuilder _builder = builder;
    private readonly Type _requestType = requestType;
    private PlacementKind _pendingKind = PlacementKind.Innermost;
    private Type? _pendingAnchor;

    public IHandlerBehaviorScope Add(Type openGenericBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(openGenericBehaviorType);
        BehaviorTypeValidator.EnsureOpenGenericPipelineBehavior(
            openGenericBehaviorType, nameof(openGenericBehaviorType));

        if (_pendingKind == PlacementKind.Before)
        {
            BehaviorTypeValidator.EnsureOpenGenericPipelineBehavior(
                _pendingAnchor!, nameof(openGenericBehaviorType));
        }

        _builder.AddHandlerBehavior(
            _requestType,
            new BehaviorPlacement(openGenericBehaviorType, _pendingKind, _pendingAnchor));

        _pendingKind = PlacementKind.Innermost;
        _pendingAnchor = null;
        return this;
    }

    public IHandlerBehaviorScope AtOutermost()
    {
        _pendingKind = PlacementKind.Outermost;
        _pendingAnchor = null;
        return this;
    }

    public IHandlerBehaviorScope InsertBefore(Type anchorOpenGenericBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(anchorOpenGenericBehaviorType);
        _pendingKind = PlacementKind.Before;
        _pendingAnchor = anchorOpenGenericBehaviorType;
        return this;
    }

    public IHandlerBehaviorScope AddLogging() => Add(typeof(LoggingBehavior<,>));

    public IHandlerBehaviorScope AddTiming() => Add(typeof(TimingBehavior<,>));

    public IHandlerBehaviorScope AddExceptionToResult() => Add(typeof(ExceptionToResultBehavior<,>));

    public IHandlerBehaviorScope AddValidation() => Add(typeof(ValidationBehavior<,>));
}
