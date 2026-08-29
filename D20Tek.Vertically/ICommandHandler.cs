using D20Tek.Functional;

namespace D20Tek.Vertically;

/// <summary>
/// Represents a handler for a command that can be executed against HandleAsync method.
/// </summary>
/// <typeparam name="TCommand">The command type that will be handled by this handler.</typeparam>
/// <typeparam name="TResult">The type of the result returned by the handler.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand
    where TResult : notnull
{
    /// <summary>
    /// Handles the specified command asynchronously.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a Result wrapping the command result.</returns>
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct = default);
}
