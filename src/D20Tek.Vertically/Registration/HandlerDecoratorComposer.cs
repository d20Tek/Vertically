namespace D20Tek.Vertically.Registration;

/// <summary>
/// Materializes handler registrations into the service collection. Each registration carries a
/// typed <see cref="HandlerComposer"/> that registers the handler and composes its ordered
/// pipeline behaviors without reflection. When a handler has no applicable behaviors, the
/// service interface is registered directly to the implementation (no decorator overhead).
/// </summary>
internal static class HandlerDecoratorComposer
{
	public static void Compose(VerticallyBuilder builder)
	{
		foreach (var registration in builder.InternalHandlerRegistrations)
		{
			registration.Composer.Compose(builder.Services, builder.GetBehaviorEntriesFor(registration));
		}
	}
}
