namespace D20Tek.Vertically.Registration;

/// <summary>
/// Fluent sub-builder for configuring global (all-handler) pipeline behaviors. Behaviors are
/// applied as decorators around handlers in registration order (outermost first) and are
/// resolved as singletons, unless they implement <see cref="IScopedBehavior"/>. Custom behaviors
/// are added via <see cref="Add{TBehavior}"/> (trim- and AOT-compatible) or
/// <see cref="Add(Type)"/> (open generic, reflection-based); built-in behaviors expose dedicated
/// convenience methods that are trim- and AOT-compatible.
/// </summary>
public interface IBehaviorRegistrationBuilder
{
	/// <summary>
	/// Adds a global behavior by its open generic type definition
	/// (for example <c>typeof(MyBehavior&lt;,&gt;)</c>). The type must implement
	/// <see cref="IPipelineBehavior{TRequest, TResult}"/>. The definition is closed for each
	/// handler with <see cref="Type.MakeGenericType(Type[])"/>, so this method is not trim- or
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
	IBehaviorRegistrationBuilder Add(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type openGenericBehaviorType);

	/// <summary>
	/// Adds a global behavior by its closed type (for example
	/// <c>MyBehavior&lt;CreateOrder.Command, OrderResponse&gt;</c>, or a non-generic class
	/// implementing <see cref="IPipelineBehavior{TRequest, TResult}"/>). The behavior is applied to
	/// every handler whose request/result pair it implements, including contravariant matches
	/// on the request type, and is skipped for other handlers. This method is trim- and
	/// AOT-compatible.
	/// </summary>
	/// <typeparam name="TBehavior">A closed type implementing <see cref="IPipelineBehavior{TRequest, TResult}"/>.</typeparam>
	/// <exception cref="ArgumentException">
	/// Thrown when <typeparamref name="TBehavior"/> does not implement
	/// <see cref="IPipelineBehavior{TRequest, TResult}"/>.
	/// </exception>
	IBehaviorRegistrationBuilder Add<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
		where TBehavior : class;

	/// <summary>Adds the built-in logging behavior (request name and outcome).</summary>
	IBehaviorRegistrationBuilder AddLogging();

	/// <summary>Adds the built-in timing behavior (elapsed time around the handler).</summary>
	IBehaviorRegistrationBuilder AddTiming();

	/// <summary>
	/// Adds the built-in behavior that maps unexpected exceptions to a failure result.
	/// </summary>
	IBehaviorRegistrationBuilder AddExceptionToResult();

	/// <summary>
	/// Adds the built-in validation behavior that runs registered <see cref="IValidator{T}"/>
	/// instances and short-circuits on validation errors.
	/// </summary>
	IBehaviorRegistrationBuilder AddValidation();
}
