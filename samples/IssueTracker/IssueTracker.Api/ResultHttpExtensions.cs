using D20Tek.Functional;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IssueTracker.Api;

/// <summary>
/// Translates a functional <see cref="Result{T}"/> into an ASP.NET Core <see cref="IResult"/>,
/// mapping success to a 2xx payload and each <see cref="Error"/> classification
/// (<see cref="ErrorType"/>) to the appropriate HTTP status code with RFC 7807 problem details.
/// </summary>
internal static class ResultHttpExtensions
{
    /// <summary>Maps a success to <c>200 OK</c> with the value, or a failure to a problem response.</summary>
    public static IResult ToOk<T>(this Result<T> result) where T : notnull =>
        result.Match(value => Results.Ok(value), ToProblem);

    /// <summary>Maps a success to <c>201 Created</c> at <paramref name="location"/>, or a failure to a problem response.</summary>
    public static IResult ToCreated<T>(this Result<T> result, Func<T, string> location) where T : notnull =>
        result.Match(value => Results.Created(location(value), value), ToProblem);

    private static IResult ToProblem(Error[] errors)
    {
        var primary = errors[0];
        var status = StatusFor(primary.Type);

        if (primary.Type == ErrorType.Validation || primary.Type == ErrorType.Invalid)
        {
            var failures = errors.GroupBy(e => e.Code, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());
            return Results.ValidationProblem(failures, statusCode: status);
        }

        return Results.Problem(
            detail: primary.Message,
            statusCode: status,
            title: primary.Code);
    }

    private static int StatusFor(int errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Invalid => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError,
    };
}
