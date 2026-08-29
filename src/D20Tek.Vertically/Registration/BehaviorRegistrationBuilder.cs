namespace D20Tek.Vertically.Registration;

internal sealed class BehaviorRegistrationBuilder(VerticallyBuilder builder) : IBehaviorRegistrationBuilder
{
    private readonly VerticallyBuilder _builder = builder;

    public IBehaviorRegistrationBuilder Add(Type openGenericBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(openGenericBehaviorType);

        if (!openGenericBehaviorType.IsGenericTypeDefinition)
        {
            throw new ArgumentException(
                $"Behavior type '{openGenericBehaviorType}' must be an open generic type " +
                "definition, e.g. typeof(MyBehavior<,>).",
                nameof(openGenericBehaviorType));
        }

        if (!ImplementsPipelineBehavior(openGenericBehaviorType))
        {
            throw new ArgumentException(
                $"Behavior type '{openGenericBehaviorType}' must implement " +
                "IPipelineBehavior<TRequest, TResult>.",
                nameof(openGenericBehaviorType));
        }

        _builder.AddGlobalBehavior(openGenericBehaviorType);
        return this;
    }

    private static bool ImplementsPipelineBehavior(Type type) =>
        type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));
}
