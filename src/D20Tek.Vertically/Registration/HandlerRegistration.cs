namespace D20Tek.Vertically.Registration;

/// <summary>
/// Internal record describing a handler registration: the closed handler service interface,
/// the implementation type, the request/result types extracted from it, how it was registered,
/// and the typed composer that materializes it into the service collection.
/// </summary>
internal sealed record HandlerRegistration(
	Type ServiceType,
	Type ImplementationType,
	Type RequestType,
	Type ResultType,
	bool IsCommand,
	RegistrationSource Source,
	HandlerComposer Composer);
