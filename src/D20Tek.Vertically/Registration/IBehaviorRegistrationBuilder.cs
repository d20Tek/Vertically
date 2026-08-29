namespace D20Tek.Vertically.Registration;

/// <summary>
/// Fluent sub-builder for configuring global (all-handler) pipeline behaviors. Behaviors are
/// applied as decorators around handlers in registration order (outermost first) and are
/// resolved as singletons. Custom behaviors are added via <see cref="Add(Type)"/>; built-in
/// behaviors expose dedicated convenience methods.
/// </summary>
public interface IBehaviorRegistrationBuilder
{
    /// <summary>
    /// Adds a global behavior by its open generic type definition
    /// (for example <c>typeof(MyBehavior&lt;,&gt;)</c>). The type must implement
    /// <see cref="D20Tek.Vertically.Pipeline.IPipelineBehavior{TRequest, TResult}"/>.
    /// </summary>
    /// <param name="openGenericBehaviorType">The open generic behavior type definition.</param>
    IBehaviorRegistrationBuilder Add(Type openGenericBehaviorType);
}
