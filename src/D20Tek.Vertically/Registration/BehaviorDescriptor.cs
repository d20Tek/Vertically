namespace D20Tek.Vertically.Registration;

/// <summary>
/// Describes a registered pipeline behavior and knows how to bind it to a specific
/// (request, result) pair from generic code, so handler composition does not need
/// <see cref="Type.MakeGenericType(Type[])"/> or reflection-based constructor calls.
/// </summary>
internal abstract class BehaviorDescriptor
{
    /// <summary>
    /// Identity used for de-duplication and as the anchor key for placement. This is the open
    /// generic definition for built-in and open-generic behaviors, and the closed type for
    /// typed behaviors.
    /// </summary>
    public abstract Type Identity { get; }

    /// <summary>
    /// Registers the closed behavior for the given request/result pair and returns a factory
    /// that resolves it, or <see langword="null"/> when the behavior does not apply to that pair.
    /// </summary>
    public abstract Func<IServiceProvider, IPipelineBehavior<TRequest, TResult>>? Bind<TRequest, TResult>(
        IServiceCollection services)
        where TRequest : notnull
        where TResult : notnull;

    /// <summary>
    /// Binds the ordered behavior entries for a handler. Global entries that do not apply to the
    /// request/result pair are skipped; per-handler entries that do not apply throw.
    /// </summary>
    public static Func<IServiceProvider, IPipelineBehavior<TRequest, TResult>>[] BindAll<TRequest, TResult>(
        IServiceCollection services,
        IReadOnlyList<BehaviorEntry> entries)
        where TRequest : notnull
        where TResult : notnull
    {
        var factories = new List<Func<IServiceProvider, IPipelineBehavior<TRequest, TResult>>>(entries.Count);
        foreach (var entry in entries)
        {
            var factory = entry.Descriptor.Bind<TRequest, TResult>(services);
            if (factory is null)
            {
                if (entry.IsRequired)
                {
                    throw new InvalidOperationException(
                        $"Behavior '{entry.Descriptor.Identity}' cannot be applied to request " +
                        $"'{typeof(TRequest)}' because it does not implement " +
                        $"'{typeof(IPipelineBehavior<TRequest, TResult>)}'.");
                }

                continue;
            }

            factories.Add(factory);
        }

        return [.. factories];
    }

    /// <summary>
    /// Registers a closed behavior type with the correct lifetime and returns its resolver.
    /// Behaviors are singletons unless they implement <see cref="IScopedBehavior"/>.
    /// </summary>
    protected static Func<IServiceProvider, IPipelineBehavior<TRequest, TResult>> RegisterBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TBehavior,
        TRequest,
        TResult>(IServiceCollection services)
        where TBehavior : class, IPipelineBehavior<TRequest, TResult>
        where TRequest : notnull
        where TResult : notnull
    {
        services.TryAdd(new ServiceDescriptor(typeof(TBehavior), typeof(TBehavior), GetLifetime(typeof(TBehavior))));
        return static provider => provider.GetRequiredService<TBehavior>();
    }

    /// <summary>Returns the service lifetime for a closed behavior type.</summary>
    protected static ServiceLifetime GetLifetime(Type behaviorType) =>
        typeof(IScopedBehavior).IsAssignableFrom(behaviorType) ? ServiceLifetime.Scoped : ServiceLifetime.Singleton;
}

/// <summary>
/// A behavior in a handler's ordered pipeline. <see cref="IsRequired"/> is <see langword="true"/>
/// for per-handler behaviors, which must apply to the handler, and <see langword="false"/> for
/// global behaviors, which are skipped when they do not apply.
/// </summary>
/// <param name="Descriptor">The behavior descriptor.</param>
/// <param name="IsRequired">Whether the behavior must apply to the handler.</param>
internal readonly record struct BehaviorEntry(BehaviorDescriptor Descriptor, bool IsRequired);
