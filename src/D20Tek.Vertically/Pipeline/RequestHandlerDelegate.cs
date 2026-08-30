namespace D20Tek.Vertically.Pipeline;

/// <summary>
/// Represents the continuation of a request pipeline: invoking it either calls the next
/// behavior in the chain or, at the innermost position, the actual handler.
/// </summary>
/// <typeparam name="TResult">The type of the result produced by the request.</typeparam>
/// <returns>A task that resolves to the <see cref="Result{TResult}"/> produced downstream.</returns>
public delegate Task<Result<TResult>> RequestHandlerDelegate<TResult>()
    where TResult : notnull;
