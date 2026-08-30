namespace D20Tek.Vertically.Registration;

/// <summary>
/// Internal record describing a discovered handler registration: the closed handler service
/// interface, the implementation type, and the request/result types extracted from it.
/// </summary>
internal sealed record HandlerRegistration(
    Type ServiceType,
    Type ImplementationType,
    Type RequestType,
    Type ResultType,
    bool IsCommand);
