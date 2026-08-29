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

    /// <summary>
    /// Returns the ordered open-generic behavior definitions that apply to a given handler,
    /// outermost first. Currently the global behaviors in registration order; per-handler
    /// behaviors are appended (innermost) in a later step.
    /// </summary>
    internal IReadOnlyList<Type> GetBehaviorDefinitionsFor(HandlerRegistration registration) => _globalBehaviors;

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
