namespace D20Tek.Vertically.Registration;

internal static class BehaviorTypeValidator
{
    public static void EnsureOpenGenericPipelineBehavior(Type type, string paramName)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsGenericTypeDefinition)
        {
            throw new ArgumentException(
                $"Behavior type '{type}' must be an open generic type definition, " +
                "e.g. typeof(MyBehavior<,>).",
                paramName);
        }

        if (!ImplementsPipelineBehavior(type))
        {
            throw new ArgumentException(
                $"Behavior type '{type}' must implement IPipelineBehavior<TRequest, TResult>.",
                paramName);
        }
    }

    private static bool ImplementsPipelineBehavior(Type type) =>
        type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));
}
