namespace D20Tek.Vertically.Registration;

/// <summary>
/// Descriptor for a closed custom behavior added through <c>Add&lt;TBehavior&gt;()</c>. The
/// behavior applies to any handler whose <see cref="IPipelineBehavior{TRequest, TResult}"/> it is
/// assignable to (including contravariant matches on the request type). Binding uses no
/// reflection beyond an assignability check, so it is trim- and AOT-safe.
/// </summary>
/// <typeparam name="TBehavior">The closed behavior type.</typeparam>
internal sealed class TypedBehaviorDescriptor<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TBehavior> : BehaviorDescriptor
    where TBehavior : class
{
    public override Type Identity => typeof(TBehavior);

    public override Func<IServiceProvider, IPipelineBehavior<TRequest, TResult>>? Bind<TRequest, TResult>(
        IServiceCollection services)
    {
        if (!typeof(IPipelineBehavior<TRequest, TResult>).IsAssignableFrom(typeof(TBehavior)))
        {
            return null;
        }

        services.TryAdd(new ServiceDescriptor(typeof(TBehavior), typeof(TBehavior), GetLifetime(typeof(TBehavior))));
        return static provider => (IPipelineBehavior<TRequest, TResult>)provider.GetRequiredService<TBehavior>();
    }
}
