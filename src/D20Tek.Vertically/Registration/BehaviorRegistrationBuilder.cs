namespace D20Tek.Vertically.Registration;

internal sealed class BehaviorRegistrationBuilder(VerticallyBuilder builder) : IBehaviorRegistrationBuilder
{
	private readonly VerticallyBuilder _builder = builder;

	[RequiresUnreferencedCode(OpenGenericBehaviorDescriptor.ReflectionMessage)]
	[RequiresDynamicCode(OpenGenericBehaviorDescriptor.ReflectionMessage)]
	public IBehaviorRegistrationBuilder Add(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type openGenericBehaviorType)
	{
		BehaviorTypeValidator.EnsureOpenGenericPipelineBehavior(openGenericBehaviorType, nameof(openGenericBehaviorType));

		var descriptor = (BehaviorDescriptor?)BuiltInBehaviorDescriptor.Find(openGenericBehaviorType)
			?? new OpenGenericBehaviorDescriptor(openGenericBehaviorType);
		return AddGlobal(descriptor);
	}

	public IBehaviorRegistrationBuilder Add<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
		where TBehavior : class
	{
		BehaviorTypeValidator.EnsurePipelineBehavior(typeof(TBehavior), nameof(TBehavior));
		return AddGlobal(new TypedBehaviorDescriptor<TBehavior>());
	}

	public IBehaviorRegistrationBuilder AddLogging() => AddGlobal(BuiltInBehaviorDescriptor.Logging);

	public IBehaviorRegistrationBuilder AddTiming() => AddGlobal(BuiltInBehaviorDescriptor.Timing);

	public IBehaviorRegistrationBuilder AddExceptionToResult() => AddGlobal(BuiltInBehaviorDescriptor.ExceptionToResult);

	public IBehaviorRegistrationBuilder AddValidation() => AddGlobal(BuiltInBehaviorDescriptor.Validation);

	private BehaviorRegistrationBuilder AddGlobal(BehaviorDescriptor descriptor)
	{
		_builder.AddGlobalBehavior(descriptor);
		return this;
	}
}
