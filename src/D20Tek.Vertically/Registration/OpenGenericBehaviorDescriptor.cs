namespace D20Tek.Vertically.Registration;

/// <summary>
/// Descriptor for a custom open generic behavior added through <c>Add(Type)</c>. Closing the
/// definition for each handler requires <see cref="Type.MakeGenericType(Type[])"/>, so this
/// path is not trim- or AOT-safe. Creating the descriptor is annotated so callers are warned;
/// use <c>Add&lt;TBehavior&gt;()</c> for AOT-compatible custom behaviors.
/// </summary>
internal sealed class OpenGenericBehaviorDescriptor : BehaviorDescriptor
{
    internal const string ReflectionMessage =
        "Open generic behaviors are closed at startup with Type.MakeGenericType. " +
        "Use Add<TBehavior>() with a closed behavior type for trim- and AOT-compatible code.";

    [RequiresUnreferencedCode(ReflectionMessage)]
    [RequiresDynamicCode(ReflectionMessage)]
    public OpenGenericBehaviorDescriptor(Type openGenericBehaviorType) => Identity = openGenericBehaviorType;

    public override Type Identity { get; }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2055",
        Justification = "Instances are only created through the annotated constructor, so callers are already warned.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072",
        Justification = "Instances are only created through the annotated constructor, so callers are already warned.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "Instances are only created through the annotated constructor, so callers are already warned.")]
    public override Func<IServiceProvider, IPipelineBehavior<TRequest, TResult>> Bind<TRequest, TResult>(
        IServiceCollection services)
    {
        var closed = Identity.MakeGenericType(typeof(TRequest), typeof(TResult));
        services.TryAdd(new ServiceDescriptor(closed, closed, GetLifetime(closed)));
        return provider => (IPipelineBehavior<TRequest, TResult>)provider.GetRequiredService(closed);
    }
}
