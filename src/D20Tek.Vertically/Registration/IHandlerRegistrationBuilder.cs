namespace D20Tek.Vertically.Registration;

/// <summary>
/// Fluent sub-builder for registering handlers and validators, either explicitly by type,
/// by <see cref="IFeature"/>, or by assembly scanning. Handlers and validators are registered
/// with a Scoped lifetime.
/// </summary>
/// <remarks>
/// For trimmed or native AOT apps, use the typed handler overloads
/// (<see cref="AddCommandHandler{THandler, TCommand, TResult}"/> and
/// <see cref="AddQueryHandler{THandler, TQuery, TResult}"/>), <see cref="AddValidator{TValidator}"/>,
/// and <see cref="AddFeature{TFeature}"/>. The single-type-argument handler overloads and the
/// assembly scanning methods rely on reflection and are annotated so the trim and AOT analyzers
/// report their use.
/// </remarks>
public interface IHandlerRegistrationBuilder
{
	/// <summary>
	/// Registers a specific command handler implementation, discovering its command and result
	/// types with reflection. Not trim- or AOT-compatible; use
	/// <see cref="AddCommandHandler{THandler, TCommand, TResult}"/> instead in trimmed or native AOT apps.
	/// </summary>
	/// <typeparam name="THandler">A type implementing <see cref="ICommandHandler{TCommand, TResult}"/>.</typeparam>
	/// <exception cref="InvalidOperationException">
	/// Thrown when <typeparamref name="THandler"/> does not implement <see cref="ICommandHandler{TCommand, TResult}"/>.
	/// </exception>
	[RequiresUnreferencedCode(HandlerTypeInspector.HandlerReflectionMessage)]
	[RequiresDynamicCode(HandlerTypeInspector.HandlerReflectionMessage)]
	IHandlerRegistrationBuilder AddCommandHandler<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>()
		where THandler : class;

	/// <summary>
	/// Registers a specific command handler implementation with its command and result types
	/// known at compile time. This overload is trim- and AOT-compatible.
	/// </summary>
	/// <typeparam name="THandler">The handler implementation type.</typeparam>
	/// <typeparam name="TCommand">The command type handled.</typeparam>
	/// <typeparam name="TResult">The command result type.</typeparam>
	IHandlerRegistrationBuilder AddCommandHandler<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler,
		TCommand,
		TResult>()
		where THandler : class, ICommandHandler<TCommand, TResult>
		where TCommand : ICommand<TResult>
		where TResult : notnull;

	/// <summary>
	/// Registers a specific query handler implementation, discovering its query and result
	/// types with reflection. Not trim- or AOT-compatible; use
	/// <see cref="AddQueryHandler{THandler, TQuery, TResult}"/> instead in trimmed or native AOT apps.
	/// </summary>
	/// <typeparam name="THandler">A type implementing <see cref="IQueryHandler{TQuery, TResult}"/>.</typeparam>
	/// <exception cref="InvalidOperationException">
	/// Thrown when <typeparamref name="THandler"/> does not implement <see cref="IQueryHandler{TQuery, TResult}"/>.
	/// </exception>
	[RequiresUnreferencedCode(HandlerTypeInspector.HandlerReflectionMessage)]
	[RequiresDynamicCode(HandlerTypeInspector.HandlerReflectionMessage)]
	IHandlerRegistrationBuilder AddQueryHandler<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>()
		where THandler : class;

	/// <summary>
	/// Registers a specific query handler implementation with its query and result types known
	/// at compile time. This overload is trim- and AOT-compatible.
	/// </summary>
	/// <typeparam name="THandler">The handler implementation type.</typeparam>
	/// <typeparam name="TQuery">The query type handled.</typeparam>
	/// <typeparam name="TResult">The query result type.</typeparam>
	IHandlerRegistrationBuilder AddQueryHandler<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler,
		TQuery,
		TResult>()
		where THandler : class, IQueryHandler<TQuery, TResult>
		where TQuery : IQuery<TResult>
		where TResult : notnull;

	/// <summary>
	/// Registers a specific validator implementation for every <see cref="IValidator{T}"/> and
	/// <see cref="IAsyncValidator{T}"/> it implements. This method is trim- and AOT-compatible.
	/// </summary>
	/// <typeparam name="TValidator">A type implementing <see cref="IValidator{T}"/> or <see cref="IAsyncValidator{T}"/>.</typeparam>
	/// <exception cref="InvalidOperationException">
	/// Thrown when <typeparamref name="TValidator"/> implements neither validator interface.
	/// </exception>
	IHandlerRegistrationBuilder AddValidator<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TValidator>()
		where TValidator : class;

	/// <summary>
	/// Creates the feature with its parameterless constructor and runs its
	/// <see cref="IFeature.Register"/> method. Registrations made inside it are tagged with
	/// <see cref="RegistrationSource.Feature"/>. This method is trim- and AOT-compatible when the
	/// feature itself uses the typed registration APIs.
	/// </summary>
	/// <typeparam name="TFeature">The feature type.</typeparam>
	IHandlerRegistrationBuilder AddFeature<TFeature>()
		where TFeature : IFeature, new();

	/// <summary>
	/// Scans the given assembly, discovering <see cref="IFeature"/> implementers first (running
	/// their registration), then registering any remaining handlers and validators found.
	/// Not trim- or AOT-compatible; use <see cref="AddFeature{TFeature}"/> and the typed
	/// registration methods instead in trimmed or native AOT apps.
	/// </summary>
	/// <param name="assembly">The assembly to scan.</param>
	[RequiresUnreferencedCode(HandlerTypeInspector.ScanReflectionMessage)]
	[RequiresDynamicCode(HandlerTypeInspector.ScanReflectionMessage)]
	IHandlerRegistrationBuilder RegisterFromAssembly(Assembly assembly);

	/// <summary>Scans multiple assemblies. See <see cref="RegisterFromAssembly"/>.</summary>
	/// <param name="assemblies">The assemblies to scan.</param>
	[RequiresUnreferencedCode(HandlerTypeInspector.ScanReflectionMessage)]
	[RequiresDynamicCode(HandlerTypeInspector.ScanReflectionMessage)]
	IHandlerRegistrationBuilder RegisterFromAssemblies(params Assembly[] assemblies);
}
