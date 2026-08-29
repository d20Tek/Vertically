namespace D20Tek.Vertically.Pipeline;

/// <summary>
/// Opt-in marker for pipeline behaviors that require a scoped lifetime because they resolve
/// scoped services (for example <see cref="IValidator{T}"/>) from the request's
/// <see cref="System.IServiceProvider"/>. Behaviors are registered as singletons by default;
/// implementing this marker causes the registration to use a scoped lifetime instead, which
/// avoids a captive-dependency bug when a behavior depends on scoped services.
/// </summary>
public interface IScopedBehavior
{
}
