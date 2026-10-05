namespace D20Tek.Vertically.Registration;

/// <summary>
/// Internal record describing a discovered validator registration: the closed validator service
/// interface, the implementation type, and how it was registered.
/// </summary>
internal sealed record ValidatorRegistration(
    Type ServiceType,
    Type ImplementationType,
    RegistrationSource Source);
