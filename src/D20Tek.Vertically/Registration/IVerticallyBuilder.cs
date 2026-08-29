namespace D20Tek.Vertically.Registration;

/// <summary>
/// Root fluent builder for configuring D20Tek.Vertically. Exposes grouped sub-builders for
/// handler registration (<see cref="Handlers"/>) and global behavior configuration
/// (<see cref="Behaviors"/>), plus direct access to the underlying
/// <see cref="IServiceCollection"/> for slice-specific service registrations.
/// </summary>
public interface IVerticallyBuilder
{
    /// <summary>The underlying service collection.</summary>
    IServiceCollection Services { get; }

    /// <summary>Handler and validator registration (explicit, scanning, and feature discovery).</summary>
    IHandlerRegistrationBuilder Handlers { get; }

    /// <summary>Global (all-handler) behavior configuration, applied in registration order.</summary>
    IBehaviorRegistrationBuilder Behaviors { get; }
}
