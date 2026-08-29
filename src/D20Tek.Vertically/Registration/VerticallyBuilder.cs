namespace D20Tek.Vertically.Registration;

/// <summary>
/// Default <see cref="IVerticallyBuilder"/> implementation. Collects handler, validator, and
/// behavior registration intent, then materializes it into the <see cref="IServiceCollection"/>
/// when <see cref="Build"/> is called.
/// </summary>
public sealed class VerticallyBuilder : IVerticallyBuilder
{
    private readonly List<HandlerRegistration> _handlers = [];
    private readonly Dictionary<Type, Type> _handlerServiceToImpl = [];
    private readonly List<(Type ServiceType, Type ImplementationType)> _validators = [];
    private readonly List<Type> _globalBehaviors = [];
    private readonly Dictionary<Type, List<BehaviorPlacement>> _handlerBehaviors = [];

    /// <summary>Initializes a new builder over the given service collection.</summary>
    /// <param name="services">The service collection to register into.</param>
    public VerticallyBuilder(IServiceCollection services)
    {
        Services = services;
        Handlers = new HandlerRegistrationBuilder(this);
        Behaviors = new BehaviorRegistrationBuilder(this);
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public IHandlerRegistrationBuilder Handlers { get; }

    /// <inheritdoc />
    public IBehaviorRegistrationBuilder Behaviors { get; }

    /// <inheritdoc />
    public IHandlerBehaviorScope ForCommand<TCommand>() => new HandlerBehaviorScope(this, typeof(TCommand));

    /// <inheritdoc />
    public IHandlerBehaviorScope ForQuery<TQuery>() => new HandlerBehaviorScope(this, typeof(TQuery));

    internal IReadOnlyList<HandlerRegistration> HandlerRegistrations => _handlers;

    internal IReadOnlyList<Type> GlobalBehaviors => _globalBehaviors;

    internal void AddHandlerRegistration(HandlerRegistration registration)
    {
        if (_handlerServiceToImpl.TryGetValue(registration.ServiceType, out var existing))
        {
            // Same (service, implementation) pair: no-op dedupe (safe across feature + scan).
            if (existing == registration.ImplementationType) return;

            throw new InvalidOperationException(
                $"A handler for '{registration.ServiceType}' is already registered as " +
                $"'{existing}'. Cannot also register '{registration.ImplementationType}'. " +
                "Only one handler may be registered per request type.");
        }

        _handlerServiceToImpl[registration.ServiceType] = registration.ImplementationType;
        _handlers.Add(registration);
    }

    internal void AddValidatorRegistration(Type serviceType, Type implementationType)
    {
        if (!_validators.Contains((serviceType, implementationType)))
        {
            _validators.Add((serviceType, implementationType));
        }
    }

    internal void AddGlobalBehavior(Type openGenericBehaviorType)
    {
        if (!_globalBehaviors.Contains(openGenericBehaviorType))
        {
            _globalBehaviors.Add(openGenericBehaviorType);
        }
    }

    internal void AddHandlerBehavior(Type requestType, BehaviorPlacement placement)
    {
        if (!_handlerBehaviors.TryGetValue(requestType, out var placements))
        {
            placements = [];
            _handlerBehaviors[requestType] = placements;
        }

        placements.Add(placement);
    }

    /// <summary>
    /// Returns the ordered open-generic behavior definitions that apply to a given handler,
    /// outermost first. Starts from the global behaviors (registration order) and merges in
    /// any per-handler behaviors, which sit innermost by default unless a placement override
    /// (<see cref="PlacementKind.Outermost"/> / <see cref="PlacementKind.Before"/>) is set.
    /// </summary>
    internal IReadOnlyList<Type> GetBehaviorDefinitionsFor(HandlerRegistration registration)
    {
        if (!_handlerBehaviors.TryGetValue(registration.RequestType, out var placements) || placements.Count == 0)
            return _globalBehaviors;

        var ordered = new List<Type>(_globalBehaviors);
        foreach (var placement in placements)
        {
            switch (placement.Kind)
            {
                case PlacementKind.Outermost:
                    ordered.Insert(0, placement.BehaviorType);
                    break;

                case PlacementKind.Before:
                    var anchorIndex = ordered.IndexOf(placement.Anchor!);
                    if (anchorIndex < 0)
                    {
                        throw new InvalidOperationException(
                            $"Cannot place behavior '{placement.BehaviorType}' before " +
                            $"'{placement.Anchor}' for request '{registration.RequestType}' " +
                            "because the anchor behavior is not part of this handler's pipeline.");
                    }

                    ordered.Insert(anchorIndex, placement.BehaviorType);
                    break;

                default:
                    ordered.Add(placement.BehaviorType);
                    break;
            }
        }

        return ordered;
    }

    /// <summary>
    /// Materializes the collected registrations into the service collection. Handlers and
    /// validators are registered as Scoped; behaviors are composed around handlers as Scoped
    /// factories with Singleton behavior instances.
    /// </summary>
    public void Build()
    {
        foreach (var (serviceType, implementationType) in _validators)
        {
            Services.TryAddScoped(serviceType, implementationType);
        }

        HandlerDecoratorComposer.Compose(this);
    }
}
