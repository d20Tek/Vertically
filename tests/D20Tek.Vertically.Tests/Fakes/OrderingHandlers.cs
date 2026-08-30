namespace D20Tek.Vertically.Tests.Fakes;

/// <summary>Command used for decorator ordering tests.</summary>
public sealed record OrderingCommand(string Value) : ICommand<string>;

/// <summary>
/// Command handler that records "Handler" to the shared <see cref="ExecutionLog"/> so ordering
/// tests can assert it runs innermost, wrapped by the behavior chain.
/// </summary>
public sealed class OrderingCommandHandler : ICommandHandler<OrderingCommand, string>
{
    public Task<Result<string>> HandleAsync(OrderingCommand command, CancellationToken cancellationToken = default)
    {
        ExecutionLog.Record("Handler");
        return Task.FromResult(Result<string>.Success(command.Value));
    }
}

/// <summary>Query used for decorator ordering tests on the query pipeline.</summary>
public sealed record OrderingQuery(string Value) : IQuery<string>;

/// <summary>
/// Query handler that records "Handler" to the shared <see cref="ExecutionLog"/> so ordering
/// tests can assert the query pipeline wraps it identically to the command pipeline.
/// </summary>
public sealed class OrderingQueryHandler : IQueryHandler<OrderingQuery, string>
{
    public Task<Result<string>> HandleAsync(OrderingQuery query, CancellationToken cancellationToken = default)
    {
        ExecutionLog.Record("Handler");
        return Task.FromResult(Result<string>.Success(query.Value));
    }
}
