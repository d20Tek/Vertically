namespace D20Tek.Vertically.Registration;

/// <summary>
/// Handler-interface adapter that composes an ordered set of <see cref="IPipelineBehavior{TRequest, TResult}"/>
/// around an inner command handler. Behaviors are applied outermost-first (index 0 is the
/// outermost); the innermost continuation invokes the real handler.
/// </summary>
internal sealed class CommandHandlerBehaviorDecorator<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IEnumerable<IPipelineBehavior<TCommand, TResult>> behaviors) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
    where TResult : notnull
{
    private readonly ICommandHandler<TCommand, TResult> _inner = inner;
    private readonly IReadOnlyList<IPipelineBehavior<TCommand, TResult>> _behaviors = 
        behaviors as IReadOnlyList<IPipelineBehavior<TCommand, TResult>> ?? [.. behaviors];

    public Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct = default)
    {
        RequestHandlerDelegate<TResult> next = () => _inner.HandleAsync(command, ct);
        for (var i = _behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = _behaviors[i];
            var downstream = next;
            next = () => behavior.HandleAsync(command, downstream, ct);
        }

        return next();
    }
}
