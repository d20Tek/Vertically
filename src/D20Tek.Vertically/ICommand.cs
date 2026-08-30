namespace D20Tek.Vertically;

/// <summary>
/// Represents a command that, when handled, produces a result of type <typeparamref name="TResult"/>.
/// Carrying the result type on the command lets handlers, behaviors, and registration discover the
/// command-to-result pairing from the command alone.
/// </summary>
/// <typeparam name="TResult">The type of the result produced when this command is handled.</typeparam>
public interface ICommand<TResult>
    where TResult : notnull
{
}
