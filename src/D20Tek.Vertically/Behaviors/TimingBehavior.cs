using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace D20Tek.Vertically.Behaviors;

/// <summary>
/// Built-in behavior that measures the elapsed time of the downstream pipeline (behaviors +
/// handler) and logs it. Kept separate from <see cref="LoggingBehavior{TRequest, TResult}"/>
/// so timing can be enabled independently. Stateless and safe to resolve as a singleton.
/// <para>
/// The logger is optional: when no <see cref="ILogger{TCategoryName}"/> is registered, the
/// behavior falls back to <see cref="NullLogger{T}"/> and continues without emitting logs
/// instead of failing to resolve.
/// </para>
/// </summary>
/// <typeparam name="TRequest">The command or query type being handled.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the request.</typeparam>
public sealed class TimingBehavior<TRequest, TResult>(
    ILogger<TimingBehavior<TRequest, TResult>>? logger = null)
    : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
    where TResult : notnull
{
    private readonly ILogger _logger = logger ?? NullLogger<TimingBehavior<TRequest, TResult>>.Instance;

    /// <inheritdoc />
    public async Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return await next().ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "{RequestName} completed in {ElapsedMilliseconds} ms.",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
