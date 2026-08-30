namespace D20Tek.Vertically.Queries.Pagination;

/// <summary>
/// Validates a <see cref="CursorPagedRequest"/> against paging guardrails: page size bounds.
/// Because <see cref="IValidator{T}"/> is contravariant, this validator also validates subclasses of
/// <see cref="CursorPagedRequest"/>.
/// </summary>
public sealed class CursorPagedRequestValidator : IValidator<CursorPagedRequest>
{
    /// <inheritdoc />
    public ValidationErrors Validate(CursorPagedRequest input)
    {
        var errors = ValidationErrors.Create();
        errors.AddIfError(() => input.PageSize < 1, "PageSize", "PageSize must be greater than or equal to 1.");
        errors.AddIfError(
            () => input.PageSize > CursorPagedRequest.MaxPageSize,
            "PageSize",
            $"PageSize must not exceed {CursorPagedRequest.MaxPageSize}.");

        return errors;
    }
}
