namespace D20Tek.Vertically.Registration;

/// <summary>
/// Default <see cref="IVerticallyBuilder"/> implementation. Collects handler, validator, and
/// behavior registration intent, then materializes it into the <see cref="IServiceCollection"/>
/// when <see cref="Build"/> is called. Consumers interact only through
/// <see cref="IVerticallyBuilder"/>; instances are created by
/// <see cref="ServiceCollectionExtensions.AddVertically"/>.
/// </summary>
internal sealed class VerticallyBuilder : IVerticallyBuilder
{
    private readonly List<HandlerRegistration> _handlers = [];
    private readonly List<HandlerRegistration> _handlerAttempts = [];
    private readonly Dictionary<Type, Type> _handlerServiceToImpl = [];
    private readonly List<ValidatorRegistration> _validators = [];
    private readonly List<ValidatorRegistration> _validatorAttempts = [];
    private readonly BehaviorRegistry _behaviorRegistry = new();

    /// <summary>Initializes a new builder over the given service collection.</summary>
    /// <param name="services">The service collection to register into.</param>
    internal VerticallyBuilder(IServiceCollection services)
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

    /// <inheritdoc />
    public IReadOnlyList<HandlerRegistrationInfo> HandlerRegistrations =>
        [.. _handlers.Select(h => new HandlerRegistrationInfo(
            h.ServiceType, h.ImplementationType, h.RequestType, h.ResultType, h.IsCommand, h.Source))];

    /// <inheritdoc />
    public IReadOnlyList<ValidatorRegistrationInfo> ValidatorRegistrations =>
        [.. _validators.Select(v => new ValidatorRegistrationInfo(v.ServiceType, v.ImplementationType, v.Source))];

    internal IReadOnlyList<HandlerRegistration> InternalHandlerRegistrations => _handlers;

    internal IReadOnlyList<ValidatorRegistration> InternalValidatorRegistrations => _validators;

    internal IReadOnlyList<HandlerRegistration> HandlerRegistrationAttempts => _handlerAttempts;

    internal IReadOnlyList<ValidatorRegistration> ValidatorRegistrationAttempts => _validatorAttempts;

    internal void AddHandlerRegistration(HandlerRegistration registration)
    {
        _handlerAttempts.Add(registration);

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

    internal void AddValidatorRegistration(ValidatorRegistration registration)
    {
        _validatorAttempts.Add(registration);

        if (!_validators.Any(v => v.ServiceType == registration.ServiceType && v.ImplementationType == registration.ImplementationType))
        {
            _validators.Add(registration);
        }
    }

    internal void AddGlobalBehavior(Type openGenericBehaviorType) =>
        _behaviorRegistry.AddGlobal(openGenericBehaviorType);

    internal void AddHandlerBehavior(Type requestType, BehaviorPlacement placement) =>
        _behaviorRegistry.AddForHandler(requestType, placement);

    internal IReadOnlyList<Type> GetBehaviorDefinitionsFor(HandlerRegistration registration) =>
        _behaviorRegistry.GetDefinitionsFor(registration);

    /// <summary>
    /// Materializes the collected registrations into the service collection. Handlers and
    /// validators are registered as Scoped; behaviors are composed around handlers as Scoped
    /// factories with Singleton behavior instances. Invoked once by
    /// <see cref="ServiceCollectionExtensions.AddVertically"/>.
    /// </summary>
    internal void Build()
    {
        foreach (var registration in _validators)
        {
            // TryAddEnumerable keys off (service type, implementation type), so multiple
            // distinct validators for the same request type all register and are resolved
            // together by ValidationBehavior via GetServices, while exact duplicate
            // (service, impl) pairs from feature + scan overlap are deduped.
            Services.TryAddEnumerable(
                ServiceDescriptor.Scoped(registration.ServiceType, registration.ImplementationType));
        }

        HandlerDecoratorComposer.Compose(this);
    }
}
