namespace D20Tek.Vertically.Tests.Fakes;

/// <summary>
/// Shared, test-scoped sink that records the order in which behaviors and handlers run.
/// Reset it at the start of each test via <see cref="Clear"/>.
/// </summary>
public static class ExecutionLog
{
    private static readonly ConcurrentQueue<string> _entries = new();

    public static void Record(string entry) => _entries.Enqueue(entry);

    public static IReadOnlyList<string> Entries => _entries.ToArray();

    public static void Clear() => _entries.Clear();
}

/// <summary>
/// Records "&lt;Name&gt;:before" before calling next and "&lt;Name&gt;:after" afterward, so tests can
/// assert wrapping order. Each concrete subclass supplies its own label.
/// </summary>
public abstract class RecordingBehaviorBase<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
    where TResult : notnull
{
    protected abstract string Name { get; }

    public async Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken = default)
    {
        ExecutionLog.Record($"{Name}:before");
        var result = await next().ConfigureAwait(false);
        ExecutionLog.Record($"{Name}:after");
        return result;
    }
}

public sealed class FirstBehavior<TRequest, TResult> : RecordingBehaviorBase<TRequest, TResult>
    where TRequest : notnull
    where TResult : notnull
{
    protected override string Name => "First";
}

public sealed class SecondBehavior<TRequest, TResult> : RecordingBehaviorBase<TRequest, TResult>
    where TRequest : notnull
    where TResult : notnull
{
    protected override string Name => "Second";
}

public sealed class ThirdBehavior<TRequest, TResult> : RecordingBehaviorBase<TRequest, TResult>
    where TRequest : notnull
    where TResult : notnull
{
    protected override string Name => "Third";
}

/// <summary>
/// Behavior that short-circuits the pipeline by returning a failure without invoking next.
/// </summary>
public sealed class ShortCircuitBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
    where TResult : notnull
{
    public const string ErrorCode = "short.circuit";

    public Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken = default)
    {
        ExecutionLog.Record("ShortCircuit:short-circuited");
        return Task.FromResult(Result<TResult>.Failure(Error.Failure(ErrorCode, "short circuited")));
    }
}

/// <summary>
/// Open generic type that does NOT implement <see cref="IPipelineBehavior{TRequest, TResult}"/>,
/// used to exercise the behavior-type validation guard.
/// </summary>
public sealed class NotABehavior<TRequest, TResult>
    where TRequest : notnull
    where TResult : notnull
{
}

/// <summary>
/// Open generic type that implements a generic interface other than
/// <see cref="IPipelineBehavior{TRequest, TResult}"/>, used to exercise the
/// <c>i.GetGenericTypeDefinition() == typeof(IPipelineBehavior&lt;,&gt;)</c> branch of the
/// behavior-type validation predicate (an interface is present, so the lambda runs, but it
/// is the wrong generic definition).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WrongInterfaceBehavior<TRequest, TResult> : IComparable<TRequest>
    where TRequest : notnull
    where TResult : notnull
{
    public int CompareTo(TRequest? other) => 0;
}

/// <summary>
/// Open generic type that implements only a NON-generic interface, used to exercise the
/// <c>i.IsGenericType</c> (false) short-circuit of the behavior-type validation predicate:
/// the lambda runs for a present interface, but <see cref="IDisposable"/> is not generic.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class NonGenericInterfaceBehavior<TRequest, TResult> : IDisposable
    where TRequest : notnull
    where TResult : notnull
{
    public void Dispose()
    {
    }
}

