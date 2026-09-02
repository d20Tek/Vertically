using D20Tek.Functional;

namespace IssueTracker.Cli;

/// <summary>
/// Console analog of the API's <c>ResultHttpExtensions</c> and the Web's <c>ResultViewExtensions</c>:
/// maps a functional <see cref="Result{T}"/> to console output plus a process exit code. Success writes
/// the rendered value to stdout and returns <c>0</c>; each <see cref="Error"/> classification
/// (<see cref="ErrorType"/>) is written to stderr and mapped to a distinct non-zero exit code so scripts
/// can react to the failure kind.
/// </summary>
internal static class ResultConsoleExtensions
{
    /// <summary>Exit code returned on success.</summary>
    public const int SuccessExitCode = 0;

    /// <summary>Exit code returned for validation/invalid-input failures.</summary>
    public const int ValidationExitCode = 2;

    /// <summary>Exit code returned for business-rule/conflict failures.</summary>
    public const int ConflictExitCode = 3;

    /// <summary>Exit code returned for not-found failures.</summary>
    public const int NotFoundExitCode = 4;

    /// <summary>Exit code returned for unclassified/unexpected failures.</summary>
    public const int FailureExitCode = 1;

    /// <summary>
    /// Renders <paramref name="result"/>: on success writes <paramref name="render"/>'s text to stdout and
    /// returns <see cref="SuccessExitCode"/>; on failure writes the error(s) to stderr and returns the
    /// exit code matching the primary error's classification.
    /// </summary>
    public static int ToConsole<T>(this Result<T> result, Func<T, string> render) where T : notnull =>
        result.Match(
            value =>
            {
                Console.Out.WriteLine(render(value));
                return SuccessExitCode;
            },
            WriteErrors);

    private static int WriteErrors(Error[] errors)
    {
        var primary = errors[0];

        if (primary.Type == ErrorType.Validation || primary.Type == ErrorType.Invalid)
        {
            Console.Error.WriteLine("Validation failed:");
            foreach (var error in errors)
            {
                Console.Error.WriteLine($"  - {error.Code}: {error.Message}");
            }

            return ValidationExitCode;
        }

        Console.Error.WriteLine($"{primary.Code}: {primary.Message}");
        return ExitCodeFor(primary.Type);
    }

    private static int ExitCodeFor(int errorType) => errorType switch
    {
        ErrorType.NotFound => NotFoundExitCode,
        ErrorType.Conflict => ConflictExitCode,
        _ => FailureExitCode,
    };
}
