namespace D20Tek.Vertically.Registration;

/// <summary>
/// Identifies how a handler or validator registration was discovered/added, so tooling and
/// diagnostics can answer "how did this registration get here?".
/// </summary>
public enum RegistrationSource
{
    /// <summary>Registered via an <see cref="IFeature"/>'s <c>Register</c> call (phase 1 discovery).</summary>
    Feature,

    /// <summary>Discovered by the loose assembly scan (phase 2 discovery).</summary>
    Scan,

    /// <summary>Registered explicitly via <see cref="IHandlerRegistrationBuilder"/> (e.g. <c>Add&lt;THandler&gt;()</c>).</summary>
    Manual,

    /// <summary>Reserved for a future source-generated registration path.</summary>
    Generated
}
