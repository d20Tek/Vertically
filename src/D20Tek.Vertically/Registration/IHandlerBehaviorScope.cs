namespace D20Tek.Vertically.Registration;

/// <summary>
/// Fluent scope for configuring behaviors on a single handler (option B). Behaviors added here
/// sit closest to the handler (innermost) by default, inside any global behaviors. Placement
/// modifiers (<see cref="AtOutermost"/>, <see cref="InsertBefore(Type)"/>) apply to the next
/// behavior added.
/// </summary>
public interface IHandlerBehaviorScope
{
    /// <summary>
    /// Adds a per-handler behavior by its open generic type definition
    /// (for example <c>typeof(MyBehavior&lt;,&gt;)</c>), using the currently pending placement
    /// (innermost by default). The type must implement
    /// <see cref="D20Tek.Vertically.Pipeline.IPipelineBehavior{TRequest, TResult}"/>.
    /// </summary>
    /// <param name="openGenericBehaviorType">The open generic behavior type definition.</param>
    IHandlerBehaviorScope Add(Type openGenericBehaviorType);

    /// <summary>Adds the built-in logging behavior for this handler.</summary>
    IHandlerBehaviorScope AddLogging();

    /// <summary>Adds the built-in timing behavior for this handler.</summary>
    IHandlerBehaviorScope AddTiming();

    /// <summary>Adds the built-in exception-to-result behavior for this handler.</summary>
    IHandlerBehaviorScope AddExceptionToResult();

    /// <summary>Adds the built-in validation behavior for this handler.</summary>
    IHandlerBehaviorScope AddValidation();

    /// <summary>
    /// Places the next added behavior outside all existing behaviors for this handler
    /// (runs first on the way in).
    /// </summary>
    IHandlerBehaviorScope AtOutermost();

    /// <summary>
    /// Places the next added behavior immediately outside the given anchor behavior in this
    /// handler's pipeline. The anchor is an open generic behavior type definition.
    /// </summary>
    /// <param name="anchorOpenGenericBehaviorType">The anchor open generic behavior type definition.</param>
    IHandlerBehaviorScope InsertBefore(Type anchorOpenGenericBehaviorType);
}
