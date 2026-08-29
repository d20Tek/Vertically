using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace D20Tek.Vertically.Behaviors;

/// <summary>
/// Built-in behavior that logs the start and outcome (success/failure) of each request.
/// Stateless and safe to resolve as a singleton. Replaceable by registering a different
/// closed <see cref="IPipelineBehavior{TRequest, TResult}"/> for the same request type before
/// the container is built.
/// <para>
/// The logger is optional: when no <see cref="ILogger{TCategoryName}"/> is registered (for
/// example a minimal app with no logging configured), the behavior falls back to
/// <see cref="NullLogger{T}"/> and continues without emitting logs instead of failing to
/// resolve.
/// </para>
/// </summary>
/// <typeparam name="TRequest">The command or query type being handled.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the request.</typeparam>
public sealed class LoggingBehavior<TRequest, TResult>(
    ILogger<LoggingBehavior<TRequest, TResult>>? logger = null)
    : IPipelineBehavior<TRequest, TResult>
    where TRequest : notnull
    where TResult : notnull
{
    private readonly ILogger _logger = logger ?? NullLogger<LoggingBehavior<TRequest, TResult>>.Instance;

    /// <inheritdoc />
    public async Task<Result<TResult>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;
        if (_logger.IsEnabled(LogLevel.Information)) _logger.LogInformation("Handling {RequestName}.", requestName);

        var result = await next().ConfigureAwait(false);

        if (result.IsSuccess)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Handled {RequestName} successfully.", requestName);
        }
        else if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(
                "Handling {RequestName} failed: {Errors}.",
                requestName,
                string.Join("; ", result.GetErrors().Select(e => $"{e.Code}: {e.Message}")));
        }

        return result;
    }
}
