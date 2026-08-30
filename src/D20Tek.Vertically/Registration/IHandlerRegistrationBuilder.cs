namespace D20Tek.Vertically.Registration;

/// <summary>
/// Fluent sub-builder for registering handlers and validators, either explicitly by type,
/// by assembly scanning, or via <see cref="IFeature"/> discovery. Handlers and validators are
/// registered with a Scoped lifetime.
/// </summary>
public interface IHandlerRegistrationBuilder
{
    /// <summary>Registers a specific command handler implementation.</summary>
    /// <typeparam name="THandler">A type implementing <see cref="ICommandHandler{TCommand, TResult}"/>.</typeparam>
    IHandlerRegistrationBuilder AddCommandHandler<THandler>() where THandler : class;

    /// <summary>Registers a specific query handler implementation.</summary>
    /// <typeparam name="THandler">A type implementing <see cref="IQueryHandler{TQuery, TResult}"/>.</typeparam>
    IHandlerRegistrationBuilder AddQueryHandler<THandler>() where THandler : class;

    /// <summary>Registers a specific validator implementation.</summary>
    /// <typeparam name="TValidator">A type implementing <see cref="IValidator{T}"/> or <see cref="IAsyncValidator{T}"/>.</typeparam>
    IHandlerRegistrationBuilder AddValidator<TValidator>() where TValidator : class;

    /// <summary>
    /// Scans the given assembly, discovering <see cref="IFeature"/> implementers first (running
    /// their registration), then registering any remaining handlers and validators found.
    /// </summary>
    IHandlerRegistrationBuilder RegisterFromAssembly(Assembly assembly);

    /// <summary>Scans multiple assemblies. See <see cref="RegisterFromAssembly"/>.</summary>
    IHandlerRegistrationBuilder RegisterFromAssemblies(params Assembly[] assemblies);
}
