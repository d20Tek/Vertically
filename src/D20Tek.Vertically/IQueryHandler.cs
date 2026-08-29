namespace D20Tek.Vertically;

/// <summary>
/// Represents a handler for a query that can be executed against HandleAsync method.
/// </summary>
/// <typeparam name="TQuery">The query type that will be handled by this handler.</typeparam>
/// <typeparam name="TResult">The type of the result returned by the handler.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : notnull
{
    /// <summary>
    /// Handles the specified query asynchronously.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result wrapping the query result.</returns>
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct = default);
}
