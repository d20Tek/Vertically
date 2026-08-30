namespace D20Tek.Vertically.Behaviors;

/// <summary>
/// Built-in behavior that catches unexpected exceptions from the downstream pipeline and maps
/// them to a failure <see cref="Result{TResult}"/> (an <c>Unexpected</c> error), so the Result
/// contract holds end-to-end. Cancellation is intentionally allowed to propagate. Stateless and
/// safe to resolve as a singleton.
/// </summary>
/// <typeparam name="TRequest">The command or query type being handled.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the request.</typeparam>
public sealed class ExceptionToResultBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
    where TResult : notnull
{
    /// <inheritdoc />
    public async Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await next().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<TResult>.Failure(ex);
        }
    }
}
