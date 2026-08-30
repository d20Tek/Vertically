namespace D20Tek.Vertically.Pipeline;

/// <summary>
/// Represents an opt-in, decorator-based pipeline behavior that wraps the execution of a
/// command or query handler. Both built-in and custom behaviors implement this single
/// contract, and behaviors are composed around the handler in registration order
/// (outermost first). A behavior may short-circuit the pipeline by returning a
/// <see cref="Result{TResult}"/> failure without invoking the <c>next</c> continuation.
/// </summary>
/// <typeparam name="TRequest">The command or query type being handled.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the request.</typeparam>
public interface IPipelineBehavior<in TRequest, TResult>
    where TRequest : notnull
    where TResult : notnull
{
    /// <summary>
    /// Handles the request, optionally invoking <paramref name="next"/> to continue the
    /// pipeline toward the handler.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The continuation that invokes the next behavior or the handler.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that resolves to the <see cref="Result{TResult}"/> for the request.</returns>
    Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken = default);
}
