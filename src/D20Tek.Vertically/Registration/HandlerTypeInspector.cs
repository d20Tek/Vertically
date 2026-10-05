namespace D20Tek.Vertically.Registration;

/// <summary>
/// Reflection helpers that inspect a concrete type for the D20Tek.Vertically contracts it
/// implements (command/query handlers and validators).
/// </summary>
internal static class HandlerTypeInspector
{
    public static IEnumerable<HandlerRegistration> GetHandlerRegistrations(
        Type implementationType,
        RegistrationSource source)
    {
        foreach (var iface in implementationType.GetInterfaces())
        {
            if (!iface.IsGenericType) continue;

            var definition = iface.GetGenericTypeDefinition();
            if (definition == typeof(ICommandHandler<,>))
            {
                var args = iface.GetGenericArguments();
                yield return new HandlerRegistration(iface, implementationType, args[0], args[1], IsCommand: true, source);
            }
            else if (definition == typeof(IQueryHandler<,>))
            {
                var args = iface.GetGenericArguments();
                yield return new HandlerRegistration(iface, implementationType, args[0], args[1], IsCommand: false, source);
            }
        }
    }

    public static IEnumerable<ValidatorRegistration> GetValidatorRegistrations(
        Type implementationType,
        RegistrationSource source)
    {
        foreach (var iface in implementationType.GetInterfaces())
        {
            if (!iface.IsGenericType) continue;

            var definition = iface.GetGenericTypeDefinition();
            if (definition == typeof(IValidator<>) || definition == typeof(IAsyncValidator<>))
            {
                yield return new ValidatorRegistration(iface, implementationType, source);
            }
        }
    }

    public static bool IsConcreteClass(Type type) =>
        type is { IsClass: true, IsAbstract: false } && !type.ContainsGenericParameters;

    public static bool IsNestedInside(Type type, IReadOnlySet<Type> declaringTypes)
    {
        for (var current = type.DeclaringType; current is not null; current = current.DeclaringType)
        {
            if (declaringTypes.Contains(current))
            {
                return true;
            }
        }

        return false;
    }
}
