namespace D20Tek.Vertically.Behaviors;

/// <summary>
/// Built-in behavior that runs registered <see cref="IValidator{T}"/> and
/// <see cref="IAsyncValidator{T}"/> instances for the request and short-circuits to a validation
/// failure <see cref="Result{TResult}"/> when any validator reports errors. Validation stays
/// explicit: this behavior only runs when validators are registered, and handlers may still
/// validate inline.
/// <para>
/// Validators are <see cref="ServiceLifetime.Scoped"/> while behaviors default to singleton, so
/// this behavior implements <see cref="IScopedBehavior"/> and resolves validators lazily from
/// the request scope inside <see cref="HandleAsync"/> (never in the constructor) to avoid a
/// captive-dependency bug.
/// </para>
/// </summary>
/// <typeparam name="TRequest">The command or query type being handled.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the request.</typeparam>
public sealed class ValidationBehavior<TRequest, TResult>(IServiceProvider serviceProvider)
    : IPipelineBehavior<TRequest, TResult>, IScopedBehavior
    where TRequest : notnull
    where TResult : notnull
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public async Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken = default)
    {
        var validators = _serviceProvider.GetServices<IValidator<TRequest>>();

        foreach (var validator in validators)
        {
            var errors = validator.Validate(request);
            if (errors.HasErrors)
            {
                return errors.ToFailure<TResult>();
            }
        }

        var asyncValidators = _serviceProvider.GetServices<IAsyncValidator<TRequest>>();

        foreach (var validator in asyncValidators)
        {
            var errors = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
            if (errors.HasErrors)
            {
                return errors.ToFailure<TResult>();
            }
        }

        return await next().ConfigureAwait(false);
    }
}
