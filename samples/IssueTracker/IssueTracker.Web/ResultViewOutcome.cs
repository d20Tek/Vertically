using D20Tek.Functional;

namespace IssueTracker.Web;

/// <summary>
/// Classifies a translated <see cref="Result{T}"/> for UI rendering. The Web analog of the HTTP
/// status codes the API host produces in <c>ResultHttpExtensions</c>.
/// </summary>
public enum ResultViewOutcome
{
    /// <summary>The operation succeeded; <see cref="ResultView{T}.Value"/> is populated.</summary>
    Success,

    /// <summary>Input validation failed; see <see cref="ResultView{T}.FieldErrors"/>.</summary>
    Validation,

    /// <summary>The requested resource was not found.</summary>
    NotFound,

    /// <summary>A business-rule/conflict failure (e.g. illegal status transition, issue closed).</summary>
    Conflict,

    /// <summary>An unexpected or otherwise unclassified failure.</summary>
    Error,
}
