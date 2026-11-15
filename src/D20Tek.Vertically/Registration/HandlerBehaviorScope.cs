namespace D20Tek.Vertically.Registration;

internal sealed class HandlerBehaviorScope(VerticallyBuilder builder, Type requestType) : IHandlerBehaviorScope
{
	private readonly VerticallyBuilder _builder = builder;
	private readonly Type _requestType = requestType;
	private PlacementKind _pendingKind = PlacementKind.Innermost;
	private Type? _pendingAnchor;

	[RequiresUnreferencedCode(OpenGenericBehaviorDescriptor.ReflectionMessage)]
	[RequiresDynamicCode(OpenGenericBehaviorDescriptor.ReflectionMessage)]
	public IHandlerBehaviorScope Add(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type openGenericBehaviorType)
	{
		BehaviorTypeValidator.EnsureOpenGenericPipelineBehavior(openGenericBehaviorType, nameof(openGenericBehaviorType));

		var descriptor = (BehaviorDescriptor?)BuiltInBehaviorDescriptor.Find(openGenericBehaviorType)
			?? new OpenGenericBehaviorDescriptor(openGenericBehaviorType);
		return AddPlaced(descriptor);
	}

	public IHandlerBehaviorScope Add<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
		where TBehavior : class
	{
		BehaviorTypeValidator.EnsurePipelineBehavior(typeof(TBehavior), nameof(TBehavior));
		return AddPlaced(new TypedBehaviorDescriptor<TBehavior>());
	}

	public IHandlerBehaviorScope AtOutermost()
	{
		_pendingKind = PlacementKind.Outermost;
		_pendingAnchor = null;
		return this;
	}

	public IHandlerBehaviorScope InsertBefore(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type anchorBehaviorType)
	{
		BehaviorTypeValidator.EnsurePipelineBehavior(anchorBehaviorType, nameof(anchorBehaviorType));
		_pendingKind = PlacementKind.Before;
		_pendingAnchor = anchorBehaviorType;
		return this;
	}

	public IHandlerBehaviorScope AddLogging() => AddPlaced(BuiltInBehaviorDescriptor.Logging);

	public IHandlerBehaviorScope AddTiming() => AddPlaced(BuiltInBehaviorDescriptor.Timing);

	public IHandlerBehaviorScope AddExceptionToResult() => AddPlaced(BuiltInBehaviorDescriptor.ExceptionToResult);

	public IHandlerBehaviorScope AddValidation() => AddPlaced(BuiltInBehaviorDescriptor.Validation);

	private HandlerBehaviorScope AddPlaced(BehaviorDescriptor descriptor)
	{
		_builder.AddHandlerBehavior(_requestType, new BehaviorPlacement(descriptor, _pendingKind, _pendingAnchor));

		_pendingKind = PlacementKind.Innermost;
		_pendingAnchor = null;
		return this;
	}
}
