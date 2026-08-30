namespace D20Tek.Vertically.Tests.Fakes;

/// <summary>
/// Spy command handler that records invocation and returns a configurable result. The default
/// behavior echoes the command value as a success.
/// </summary>
public sealed class SampleCommandHandler : ICommandHandler<SampleCommand, string>
{
    public int CallCount { get; private set; }

    public SampleCommand? LastCommand { get; private set; }

    public Func<SampleCommand, Result<string>>? ResultFactory { get; set; }

    [ExcludeFromCodeCoverage]
    public Task<Result<string>> HandleAsync(SampleCommand command, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastCommand = command;

        var result = ResultFactory?.Invoke(command) ?? Result<string>.Success(command.Value);
        return Task.FromResult(result);
    }
}

/// <summary>Spy command handler for <see cref="OtherCommand"/>.</summary>
public sealed class OtherCommandHandler : ICommandHandler<OtherCommand, int>
{
    public int CallCount { get; private set; }

    public Task<Result<int>> HandleAsync(OtherCommand command, CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(Result<int>.Success(command.Value));
    }
}

/// <summary>Spy query handler that echoes the query value as a success by default.</summary>
public sealed class SampleQueryHandler : IQueryHandler<SampleQuery, string>
{
    public int CallCount { get; private set; }

    public SampleQuery? LastQuery { get; private set; }

    public Func<SampleQuery, Result<string>>? ResultFactory { get; set; }

    [ExcludeFromCodeCoverage]
    public Task<Result<string>> HandleAsync(SampleQuery query, CancellationToken ct = default)
    {
        CallCount++;
        LastQuery = query;

        var result = ResultFactory?.Invoke(query) ?? Result<string>.Success(query.Value);
        return Task.FromResult(result);
    }
}

/// <summary>Command handler that always throws, to exercise the ExceptionToResult behavior.</summary>
public sealed class ThrowingCommandHandler : ICommandHandler<ThrowingCommand, string>
{
    public Exception ExceptionToThrow { get; set; } = new InvalidOperationException("boom");

    public Task<Result<string>> HandleAsync(ThrowingCommand command, CancellationToken cancellationToken = default) =>
        throw ExceptionToThrow;
}

