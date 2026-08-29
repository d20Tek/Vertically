namespace D20Tek.Vertically.Registration;

/// <summary>
/// Hand-rolled open-generic decorator wiring. For each registered handler it registers the
/// concrete handler (Scoped), closes and registers the ordered behavior types (Singleton), and
/// registers the handler service interface as a Scoped factory that composes the behavior chain
/// around the handler. When a handler has no behaviors, the service interface is registered
/// directly to the implementation (no decorator overhead).
/// </summary>
internal static class HandlerDecoratorComposer
{
    public static void Compose(VerticallyBuilder builder)
    {
        var services = builder.Services;

        foreach (var registration in builder.HandlerRegistrations)
        {
            services.TryAddScoped(registration.ImplementationType);

            var behaviorDefinitions = builder.GetBehaviorDefinitionsFor(registration);
            if (behaviorDefinitions.Count == 0)
            {
                services.TryAddScoped(registration.ServiceType, registration.ImplementationType);
                continue;
            }

            ComposeWithBehaviors(services, registration, behaviorDefinitions);
        }
    }

    private static void ComposeWithBehaviors(
        IServiceCollection services,
        HandlerRegistration registration,
        IReadOnlyList<Type> behaviorDefinitions)
    {
        var behaviorInterface = typeof(IPipelineBehavior<,>)
            .MakeGenericType(registration.RequestType, registration.ResultType);

        var closedBehaviorTypes = new Type[behaviorDefinitions.Count];
        for (var i = 0; i < behaviorDefinitions.Count; i++)
        {
            var closed = behaviorDefinitions[i]
                .MakeGenericType(registration.RequestType, registration.ResultType);
            closedBehaviorTypes[i] = closed;
            services.TryAddSingleton(closed);
        }

        var decoratorType = (registration.IsCommand
                ? typeof(CommandHandlerBehaviorDecorator<,>)
                : typeof(QueryHandlerBehaviorDecorator<,>))
            .MakeGenericType(registration.RequestType, registration.ResultType);
        var constructor = decoratorType.GetConstructors()[0];

        var implementationType = registration.ImplementationType;

        object Factory(IServiceProvider provider)
        {
            var inner = provider.GetRequiredService(implementationType);
            var behaviors = Array.CreateInstance(behaviorInterface, closedBehaviorTypes.Length);
            for (var i = 0; i < closedBehaviorTypes.Length; i++)
            {
                behaviors.SetValue(provider.GetRequiredService(closedBehaviorTypes[i]), i);
            }

            return constructor.Invoke([inner, behaviors]);
        }

        services.Add(new ServiceDescriptor(registration.ServiceType, Factory, ServiceLifetime.Scoped));
    }
}
