namespace D20Tek.Vertically.Registration;

internal static class BehaviorTypeValidator
{
	public static void EnsureOpenGenericPipelineBehavior(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
		string paramName)
	{
		ArgumentNullException.ThrowIfNull(type);

		if (!type.IsGenericTypeDefinition)
		{
			throw new ArgumentException(
				$"Behavior type '{type}' must be an open generic type definition, " +
				"e.g. typeof(MyBehavior<,>).",
				paramName);
		}

		EnsurePipelineBehavior(type, paramName);
	}

	public static void EnsurePipelineBehavior(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
		string paramName)
	{
		ArgumentNullException.ThrowIfNull(type);

		if (!ImplementsPipelineBehavior(type))
		{
			throw new ArgumentException(
				$"Behavior type '{type}' must implement IPipelineBehavior<TRequest, TResult>.",
				paramName);
		}
	}

	private static bool ImplementsPipelineBehavior(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type) =>
		Array.Exists(type.GetInterfaces(), i =>
			i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));
}
