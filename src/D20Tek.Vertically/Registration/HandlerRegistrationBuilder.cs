namespace D20Tek.Vertically.Registration;

internal sealed class HandlerRegistrationBuilder(VerticallyBuilder builder) : IHandlerRegistrationBuilder
{
    private readonly VerticallyBuilder _builder = builder;

    public IHandlerRegistrationBuilder AddCommandHandler<THandler>() where THandler : class =>
        AddHandler(typeof(THandler), expectedCommand: true);

    public IHandlerRegistrationBuilder AddQueryHandler<THandler>() where THandler : class =>
        AddHandler(typeof(THandler), expectedCommand: false);

    public IHandlerRegistrationBuilder AddValidator<TValidator>() where TValidator : class
    {
        var registrations = HandlerTypeInspector.GetValidatorRegistrations(typeof(TValidator)).ToArray();
        if (registrations.Length == 0)
        {
            throw new InvalidOperationException($"Type '{typeof(TValidator)}' does not implement IValidator<T>.");
        }

        foreach (var (serviceType, implementationType) in registrations)
        {
            _builder.AddValidatorRegistration(serviceType, implementationType);
        }

        return this;
    }

    public IHandlerRegistrationBuilder RegisterFromAssembly(Assembly assembly)
    {
        var types = assembly.GetTypes().Where(HandlerTypeInspector.IsConcreteClass).ToArray();

        // Phase 1: features first — let each feature register itself.
        var featureTypes = types.Where(t => typeof(IFeature).IsAssignableFrom(t)).ToArray();

        foreach (var featureType in featureTypes)
        {
            var feature = (IFeature)Activator.CreateInstance(featureType)!;
            feature.Register(_builder);
        }

        // Phase 2: loose scan — register remaining handlers/validators, skipping feature-owned types.
        var featureSet = new HashSet<Type>(featureTypes);
        foreach (var type in types)
        {
            if (featureSet.Contains(type) || HandlerTypeInspector.IsNestedInside(type, featureSet)) continue;

            foreach (var registration in HandlerTypeInspector.GetHandlerRegistrations(type))
            {
                _builder.AddHandlerRegistration(registration);
            }

            foreach (var (serviceType, implementationType) in HandlerTypeInspector.GetValidatorRegistrations(type))
            {
                _builder.AddValidatorRegistration(serviceType, implementationType);
            }
        }

        return this;
    }

    public IHandlerRegistrationBuilder RegisterFromAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            RegisterFromAssembly(assembly);
        }

        return this;
    }

    private HandlerRegistrationBuilder AddHandler(Type implementationType, bool expectedCommand)
    {
        var registrations = HandlerTypeInspector.GetHandlerRegistrations(implementationType)
            .Where(r => r.IsCommand == expectedCommand).ToArray();

        if (registrations.Length == 0)
        {
            var expected = expectedCommand ? "ICommandHandler<TCommand, TResult>" : "IQueryHandler<TQuery, TResult>";
            throw new InvalidOperationException($"Type '{implementationType}' does not implement {expected}.");
        }

        foreach (var registration in registrations)
        {
            _builder.AddHandlerRegistration(registration);
        }

        return this;
    }
}
