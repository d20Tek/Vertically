namespace D20Tek.Vertically.Registration;

/// <summary>
/// Fluent scope for configuring behaviors on a single handler. Behaviors added here sit closest
/// to the handler (innermost) by default, inside any global behaviors. Placement modifiers
/// (<see cref="AtOutermost"/>, <see cref="InsertBefore(Type)"/>) apply to the next behavior added.
/// </summary>
public interface IHandlerBehaviorScope
{
	/// <summary>
	/// Adds a per-handler behavior by its open generic type definition
	/// (for example <c>typeof(MyBehavior&lt;,&gt;)</c>), using the currently pending placement
	/// (innermost by default). The type must implement
	/// <see cref="IPipelineBehavior{TRequest, TResult}"/>. The definition is closed with
	/// <see cref="Type.MakeGenericType(Type[])"/>, so this method is not trim- or
	/// AOT-compatible; use <see cref="Add{TBehavior}"/> instead in trimmed or native AOT apps.
	/// </summary>
	/// <param name="openGenericBehaviorType">The open generic behavior type definition.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="openGenericBehaviorType"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="openGenericBehaviorType"/> is not an open generic type definition
	/// implementing <see cref="IPipelineBehavior{TRequest, TResult}"/>.
	/// </exception>
	[RequiresUnreferencedCode(OpenGenericBehaviorDescriptor.ReflectionMessage)]
	[RequiresDynamicCode(OpenGenericBehaviorDescriptor.ReflectionMessage)]
	IHandlerBehaviorScope Add(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type openGenericBehaviorType);

	/// <summary>
	/// Adds a per-handler behavior by its closed type (for example
	/// <c>MyBehavior&lt;CreateOrder.Command, OrderResponse&gt;</c>), using the currently pending
	/// placement (innermost by default). The behavior must implement the handler's
	/// <see cref="IPipelineBehavior{TRequest, TResult}"/> (contravariant matches on the request
	/// type are allowed); otherwise <c>AddVertically</c> throws
	/// <see cref="InvalidOperationException"/>. This method is trim- and AOT-compatible.
	/// </summary>
	/// <typeparam name="TBehavior">A closed type implementing <see cref="IPipelineBehavior{TRequest, TResult}"/>.</typeparam>
	/// <exception cref="ArgumentException">
	/// Thrown when <typeparamref name="TBehavior"/> does not implement
	/// <see cref="IPipelineBehavior{TRequest, TResult}"/>.
	/// </exception>
	IHandlerBehaviorScope Add<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
		where TBehavior : class;

	/// <summary>Adds the built-in logging behavior for this handler.</summary>
	IHandlerBehaviorScope AddLogging();

	/// <summary>Adds the built-in timing behavior for this handler.</summary>
	IHandlerBehaviorScope AddTiming();

	/// <summary>Adds the built-in exception-to-result behavior for this handler.</summary>
	IHandlerBehaviorScope AddExceptionToResult();

	/// <summary>Adds the built-in validation behavior for this handler.</summary>
	IHandlerBehaviorScope AddValidation();

	/// <summary>
	/// Places the next added behavior outside all existing behaviors for this handler
	/// (runs first on the way in).
	/// </summary>
	IHandlerBehaviorScope AtOutermost();

	/// <summary>
	/// Places the next added behavior immediately outside the given anchor behavior in this
	/// handler's pipeline. The anchor is the open generic definition of a built-in or
	/// <see cref="Add(Type)"/> behavior (for example <c>typeof(LoggingBehavior&lt;,&gt;)</c>), or the
	/// closed type of an <see cref="Add{TBehavior}"/> behavior. This method is trim- and
	/// AOT-compatible.
	/// </summary>
	/// <param name="anchorBehaviorType">The anchor behavior type.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="anchorBehaviorType"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="anchorBehaviorType"/> does not implement
	/// <see cref="IPipelineBehavior{TRequest, TResult}"/>.
	/// </exception>
	IHandlerBehaviorScope InsertBefore(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type anchorBehaviorType);
}
