namespace D20Tek.Vertically.Registration;

/// <summary>
/// Read-only snapshot of a discovered handler registration, exposed for diagnostics and tooling
/// via <see cref="IVerticallyBuilder.HandlerRegistrations"/>.
/// </summary>
/// <param name="ServiceType">The closed handler service interface (e.g. <c>ICommandHandler&lt;TCommand, TResult&gt;</c>).</param>
/// <param name="ImplementationType">The concrete handler implementation type.</param>
/// <param name="RequestType">The command or query request type.</param>
/// <param name="ResultType">The handler's result type.</param>
/// <param name="IsCommand"><see langword="true"/> for a command handler; <see langword="false"/> for a query handler.</param>
/// <param name="Source">How this registration was discovered/added.</param>
public sealed record HandlerRegistrationInfo(
    Type ServiceType,
    Type ImplementationType,
    Type RequestType,
    Type ResultType,
    bool IsCommand,
    RegistrationSource Source);
