namespace D20Tek.Vertically.Registration;

/// <summary>
/// Helpers that inspect a concrete type for the D20Tek.Vertically contracts it implements
/// (command/query handlers and validators) and build the matching registrations.
/// </summary>
internal static class HandlerTypeInspector
{
	internal const string HandlerReflectionMessage =
		"Handlers registered without their request and result types are composed by closing " +
		"generic types at startup with Type.MakeGenericType. Use AddCommandHandler<THandler, TCommand, TResult>() " +
		"or AddQueryHandler<THandler, TQuery, TResult>() for trim- and AOT-compatible code.";

	internal const string ScanReflectionMessage =
		"Assembly scanning discovers types with reflection, which is not compatible with trimming " +
		"or native AOT. Register features with AddFeature<TFeature>() and handlers with the typed " +
		"AddCommandHandler/AddQueryHandler overloads instead.";

	public static HandlerRegistration CreateCommandRegistration<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler,
		TCommand,
		TResult>(RegistrationSource source)
		where THandler : class, ICommandHandler<TCommand, TResult>
		where TCommand : ICommand<TResult>
		where TResult : notnull =>
		new(
			typeof(ICommandHandler<TCommand, TResult>),
			typeof(THandler),
			typeof(TCommand),
			typeof(TResult),
			IsCommand: true,
			source,
			new CommandHandlerComposer<THandler, TCommand, TResult>());

	public static HandlerRegistration CreateQueryRegistration<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler,
		TQuery,
		TResult>(RegistrationSource source)
		where THandler : class, IQueryHandler<TQuery, TResult>
		where TQuery : IQuery<TResult>
		where TResult : notnull =>
		new(
			typeof(IQueryHandler<TQuery, TResult>),
			typeof(THandler),
			typeof(TQuery),
			typeof(TResult),
			IsCommand: false,
			source,
			new QueryHandlerComposer<THandler, TQuery, TResult>());

	[RequiresUnreferencedCode(HandlerReflectionMessage)]
	[RequiresDynamicCode(HandlerReflectionMessage)]
	public static IEnumerable<HandlerRegistration> GetHandlerRegistrations(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type implementationType,
		RegistrationSource source)
	{
		foreach (var iface in implementationType.GetInterfaces())
		{
			if (!iface.IsGenericType) continue;

			var definition = iface.GetGenericTypeDefinition();
			bool isCommand;
			Type composerDefinition;
			if (definition == typeof(ICommandHandler<,>))
			{
				isCommand = true;
				composerDefinition = typeof(CommandHandlerComposer<,,>);
			}
			else if (definition == typeof(IQueryHandler<,>))
			{
				isCommand = false;
				composerDefinition = typeof(QueryHandlerComposer<,,>);
			}
			else
			{
				continue;
			}

			var args = iface.GetGenericArguments();
			var composer = (HandlerComposer)Activator.CreateInstance(
				composerDefinition.MakeGenericType(implementationType, args[0], args[1]))!;
			yield return new HandlerRegistration(iface, implementationType, args[0], args[1], isCommand, source, composer);
		}
	}

	public static IEnumerable<ValidatorRegistration> GetValidatorRegistrations(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] Type implementationType,
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
