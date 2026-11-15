namespace D20Tek.Vertically.Registration;

internal sealed class HandlerRegistrationBuilder(VerticallyBuilder builder) : IHandlerRegistrationBuilder
{
	private readonly VerticallyBuilder _builder = builder;
	private RegistrationSource _currentSource = RegistrationSource.Manual;

	[RequiresUnreferencedCode(HandlerTypeInspector.HandlerReflectionMessage)]
	[RequiresDynamicCode(HandlerTypeInspector.HandlerReflectionMessage)]
	public IHandlerRegistrationBuilder AddCommandHandler<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>()
		where THandler : class =>
		AddHandler(typeof(THandler), expectedCommand: true);

	public IHandlerRegistrationBuilder AddCommandHandler<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler,
		TCommand,
		TResult>()
		where THandler : class, ICommandHandler<TCommand, TResult>
		where TCommand : ICommand<TResult>
		where TResult : notnull
	{
		_builder.AddHandlerRegistration(
			HandlerTypeInspector.CreateCommandRegistration<THandler, TCommand, TResult>(_currentSource));
		return this;
	}

	[RequiresUnreferencedCode(HandlerTypeInspector.HandlerReflectionMessage)]
	[RequiresDynamicCode(HandlerTypeInspector.HandlerReflectionMessage)]
	public IHandlerRegistrationBuilder AddQueryHandler<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>()
		where THandler : class =>
		AddHandler(typeof(THandler), expectedCommand: false);

	public IHandlerRegistrationBuilder AddQueryHandler<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler,
		TQuery,
		TResult>()
		where THandler : class, IQueryHandler<TQuery, TResult>
		where TQuery : IQuery<TResult>
		where TResult : notnull
	{
		_builder.AddHandlerRegistration(
			HandlerTypeInspector.CreateQueryRegistration<THandler, TQuery, TResult>(_currentSource));
		return this;
	}

	public IHandlerRegistrationBuilder AddValidator<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TValidator>()
		where TValidator : class
	{
		var registrations = HandlerTypeInspector
			.GetValidatorRegistrations(typeof(TValidator), _currentSource)
			.ToArray();
		if (registrations.Length == 0)
		{
			throw new InvalidOperationException($"Type '{typeof(TValidator)}' does not implement IValidator<T> or IAsyncValidator<T>.");
		}

		foreach (var registration in registrations)
		{
			_builder.AddValidatorRegistration(registration);
		}

		return this;
	}

	public IHandlerRegistrationBuilder AddFeature<TFeature>()
		where TFeature : IFeature, new()
	{
		RunFeature(new TFeature());
		return this;
	}

	[RequiresUnreferencedCode(HandlerTypeInspector.ScanReflectionMessage)]
	[RequiresDynamicCode(HandlerTypeInspector.ScanReflectionMessage)]
	public IHandlerRegistrationBuilder RegisterFromAssembly(Assembly assembly)
	{
		var types = assembly.GetTypes().Where(HandlerTypeInspector.IsConcreteClass).ToArray();

		// Phase 1: features first - let each feature register itself.
		var featureTypes = types.Where(t => typeof(IFeature).IsAssignableFrom(t)).ToArray();
		foreach (var featureType in featureTypes)
		{
			RunFeature((IFeature)Activator.CreateInstance(featureType)!);
		}

		// Phase 2: loose scan - register remaining handlers/validators, skipping feature-owned types.
		var featureSet = new HashSet<Type>(featureTypes);
		foreach (var type in types)
		{
			if (featureSet.Contains(type) || HandlerTypeInspector.IsNestedInside(type, featureSet)) continue;

			foreach (var registration in HandlerTypeInspector.GetHandlerRegistrations(type, RegistrationSource.Scan))
			{
				_builder.AddHandlerRegistration(registration);
			}

			foreach (var registration in HandlerTypeInspector.GetValidatorRegistrations(type, RegistrationSource.Scan))
			{
				_builder.AddValidatorRegistration(registration);
			}
		}

		return this;
	}

	[RequiresUnreferencedCode(HandlerTypeInspector.ScanReflectionMessage)]
	[RequiresDynamicCode(HandlerTypeInspector.ScanReflectionMessage)]
	public IHandlerRegistrationBuilder RegisterFromAssemblies(params Assembly[] assemblies)
	{
		foreach (var assembly in assemblies)
		{
			RegisterFromAssembly(assembly);
		}

		return this;
	}

	private void RunFeature(IFeature feature)
	{
		var previousSource = _currentSource;
		_currentSource = RegistrationSource.Feature;
		try
		{
			feature.Register(_builder);
		}
		finally
		{
			_currentSource = previousSource;
		}
	}

	[RequiresUnreferencedCode(HandlerTypeInspector.HandlerReflectionMessage)]
	[RequiresDynamicCode(HandlerTypeInspector.HandlerReflectionMessage)]
	private HandlerRegistrationBuilder AddHandler(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type implementationType,
		bool expectedCommand)
	{
		var registrations = HandlerTypeInspector.GetHandlerRegistrations(implementationType, _currentSource)
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
