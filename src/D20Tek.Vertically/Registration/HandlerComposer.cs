namespace D20Tek.Vertically.Registration;

/// <summary>
/// Materializes a single handler registration into the service collection using generic code
/// that knows the closed handler, request, and result types at compile time. This replaces
/// <see cref="Type.MakeGenericType(Type[])"/> and reflection-based constructor calls when
/// composing pipeline behaviors, so composition is trim- and AOT-safe.
/// </summary>
internal abstract class HandlerComposer
{
    /// <summary>
    /// Registers the concrete handler and its service interface. When behaviors apply, the
    /// interface is registered as a scoped factory that wraps the handler in a typed decorator.
    /// </summary>
    public abstract void Compose(IServiceCollection services, IReadOnlyList<BehaviorEntry> behaviors);
}

internal sealed class CommandHandlerComposer<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler,
    TCommand,
    TResult> : HandlerComposer
    where THandler : class, ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
    where TResult : notnull
{
    public override void Compose(IServiceCollection services, IReadOnlyList<BehaviorEntry> behaviors)
    {
        services.TryAddScoped<THandler>();

        var factories = BehaviorDescriptor.BindAll<TCommand, TResult>(services, behaviors);
        if (factories.Length == 0)
        {
            services.TryAddScoped<ICommandHandler<TCommand, TResult>, THandler>();
            return;
        }

        services.AddScoped<ICommandHandler<TCommand, TResult>>(provider =>
            new CommandHandlerBehaviorDecorator<TCommand, TResult>(
                provider.GetRequiredService<THandler>(),
                ResolveAll(provider, factories)));
    }

    private static IPipelineBehavior<TCommand, TResult>[] ResolveAll(
        IServiceProvider provider,
        Func<IServiceProvider, IPipelineBehavior<TCommand, TResult>>[] factories)
    {
        var behaviors = new IPipelineBehavior<TCommand, TResult>[factories.Length];
        for (var i = 0; i < factories.Length; i++)
        {
            behaviors[i] = factories[i](provider);
        }

        return behaviors;
    }
}

internal sealed class QueryHandlerComposer<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler,
    TQuery,
    TResult> : HandlerComposer
    where THandler : class, IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : notnull
{
    public override void Compose(IServiceCollection services, IReadOnlyList<BehaviorEntry> behaviors)
    {
        services.TryAddScoped<THandler>();

        var factories = BehaviorDescriptor.BindAll<TQuery, TResult>(services, behaviors);
        if (factories.Length == 0)
        {
            services.TryAddScoped<IQueryHandler<TQuery, TResult>, THandler>();
            return;
        }

        services.AddScoped<IQueryHandler<TQuery, TResult>>(provider =>
            new QueryHandlerBehaviorDecorator<TQuery, TResult>(
                provider.GetRequiredService<THandler>(),
                ResolveAll(provider, factories)));
    }

    private static IPipelineBehavior<TQuery, TResult>[] ResolveAll(
        IServiceProvider provider,
        Func<IServiceProvider, IPipelineBehavior<TQuery, TResult>>[] factories)
    {
        var behaviors = new IPipelineBehavior<TQuery, TResult>[factories.Length];
        for (var i = 0; i < factories.Length; i++)
        {
            behaviors[i] = factories[i](provider);
        }

        return behaviors;
    }
}
