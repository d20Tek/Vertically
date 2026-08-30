using D20Tek.Vertically.Registration;

namespace D20Tek.Vertically;

/// <summary>
/// Optional contract that lets a vertical slice bundle its command/query, handler, validator,
/// and any slice-specific service registrations into a single self-registering unit.
/// Implementations are discovered during assembly scanning (before the loose handler scan),
/// instantiated via their parameterless constructor, and asked to register themselves.
/// </summary>
/// <remarks>
/// Because static classes cannot implement interfaces, a feature that implements
/// <see cref="IFeature"/> is a non-static class (its <c>Command</c>/<c>Handler</c>/
/// <c>Validator</c> types may still be nested inside it). For AOT/trim safety, the
/// <see cref="Register"/> body should use explicit generic registration and avoid nested
/// assembly scans.
/// </remarks>
public interface IFeature
{
    /// <summary>
    /// Registers this feature's handlers, validators, behaviors, and services against the
    /// supplied builder.
    /// </summary>
    /// <param name="builder">The Vertically registration builder.</param>
    void Register(IVerticallyBuilder builder);
}
