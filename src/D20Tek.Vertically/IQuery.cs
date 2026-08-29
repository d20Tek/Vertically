namespace D20Tek.Vertically;

/// <summary>
/// Represents a query that, when handled, produces a result of type <typeparamref name="TResult"/>.
/// Carrying the result type on the query lets handlers, behaviors, and registration discover the
/// query-to-result pairing from the query alone.
/// </summary>
/// <typeparam name="TResult">The type of the result produced when this query is handled.</typeparam>
public interface IQuery<TResult>
    where TResult : notnull
{
}
