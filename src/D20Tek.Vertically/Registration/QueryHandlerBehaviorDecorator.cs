namespace D20Tek.Vertically.Registration;

/// <summary>
/// Handler-interface adapter that composes an ordered set of <see cref="IPipelineBehavior{TRequest, TResult}"/>
/// around an inner query handler. Behaviors are applied outermost-first (index 0 is the
/// outermost); the innermost continuation invokes the real handler.
/// </summary>
internal sealed class QueryHandlerBehaviorDecorator<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    IEnumerable<IPipelineBehavior<TQuery, TResult>> behaviors) : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : notnull
{
    private readonly IQueryHandler<TQuery, TResult> _inner = inner;
    private readonly IReadOnlyList<IPipelineBehavior<TQuery, TResult>> _behaviors = 
        behaviors as IReadOnlyList<IPipelineBehavior<TQuery, TResult>> ?? [.. behaviors];

    public Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct = default)
    {
        RequestHandlerDelegate<TResult> next = () => _inner.HandleAsync(query, ct);
        for (var i = _behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = _behaviors[i];
            var downstream = next;
            next = () => behavior.HandleAsync(query, downstream, ct);
        }

        return next();
    }
}
