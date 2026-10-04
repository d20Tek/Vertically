namespace D20Tek.Vertically.Registration;

/// <summary>
/// Read-only snapshot of a discovered validator registration, exposed for diagnostics and
/// tooling via <see cref="IVerticallyBuilder.ValidatorRegistrations"/>.
/// </summary>
/// <param name="ServiceType">The closed validator service interface (e.g. <c>IValidator&lt;TRequest&gt;</c>).</param>
/// <param name="ImplementationType">The concrete validator implementation type.</param>
/// <param name="Source">How this registration was discovered/added.</param>
public sealed record ValidatorRegistrationInfo(
    Type ServiceType,
    Type ImplementationType,
    RegistrationSource Source);
