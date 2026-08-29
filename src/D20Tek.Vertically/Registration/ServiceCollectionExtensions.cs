namespace D20Tek.Vertically.Registration;

/// <summary>
/// <see cref="IServiceCollection"/> extensions that provide the branded entry point for
/// configuring D20Tek.Vertically.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers D20Tek.Vertically handlers, validators, and pipeline behaviors into the
    /// service collection. Configure registration through the supplied
    /// <paramref name="configure"/> callback (for example
    /// <c>builder.Handlers.RegisterFromAssembly(...)</c> and <c>builder.Behaviors.AddLogging()</c>).
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">A callback that configures handlers, validators, and behaviors.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddVertically(this IServiceCollection services, Action<IVerticallyBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new VerticallyBuilder(services);
        configure(builder);
        builder.Build();

        return services;
    }
}
