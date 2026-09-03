using D20Tek.Functional;

namespace IssueTracker.Web;

/// <summary>
/// UI-facing projection of a functional <see cref="Result{T}"/>. Components inspect
/// <see cref="Outcome"/> to decide how to render: bind <see cref="Value"/> on success, show
/// <see cref="FieldErrors"/> inline in forms, or surface <see cref="Message"/> as a not-found state
/// or error/toast message.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
public sealed record ResultView<T>
{
    /// <summary>How the underlying result was classified for the UI.</summary>
    public required ResultViewOutcome Outcome { get; init; }

    /// <summary>The success value, or <see langword="null"/> when the result failed.</summary>
    public T? Value { get; init; }

    /// <summary>A single summary message for non-validation failures (empty on success).</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Per-field validation messages keyed by error code (field name). Empty unless
    /// <see cref="Outcome"/> is <see cref="ResultViewOutcome.Validation"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; init; } = new Dictionary<string, string[]>();

    /// <summary><see langword="true"/> when <see cref="Outcome"/> is <see cref="ResultViewOutcome.Success"/>.</summary>
    public bool IsSuccess => Outcome == ResultViewOutcome.Success;
}

/// <summary>
/// Translates a functional <see cref="Result{T}"/> into a <see cref="ResultView{T}"/> for Blazor
/// components — the UI counterpart to the API host's <c>ResultHttpExtensions</c>. Success maps to a
/// bound value; each <see cref="Error"/> classification (<see cref="ErrorType"/>) maps to an
/// appropriate UI outcome (inline validation, not-found state, or an error message).
/// </summary>
public static class ResultViewExtensions
{
    /// <summary>Projects a <see cref="Result{T}"/> into its UI-facing <see cref="ResultView{T}"/>.</summary>
    public static ResultView<T> ToView<T>(this Result<T> result) where T : notnull =>
        result.Match(
            value => new ResultView<T> { Outcome = ResultViewOutcome.Success, Value = value },
            ToFailureView<T>);

    private static ResultView<T> ToFailureView<T>(Error[] errors)
    {
        var primary = errors[0];

        if (primary.Type == ErrorType.Validation || primary.Type == ErrorType.Invalid)
        {
            var fieldErrors = errors.GroupBy(e => e.Code, StringComparer.Ordinal)
                                    .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());
            return new ResultView<T>
            {
                Outcome = ResultViewOutcome.Validation,
                Message = primary.Message,
                FieldErrors = fieldErrors,
            };
        }

        return new ResultView<T>
        {
            Outcome = OutcomeFor(primary.Type),
            Message = primary.Message,
        };
    }

    private static ResultViewOutcome OutcomeFor(int errorType) => errorType switch
    {
        ErrorType.NotFound => ResultViewOutcome.NotFound,
        ErrorType.Conflict => ResultViewOutcome.Conflict,
        _ => ResultViewOutcome.Error,
    };
}
