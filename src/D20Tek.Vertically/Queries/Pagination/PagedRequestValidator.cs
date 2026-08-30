namespace D20Tek.Vertically.Queries.Pagination;

/// <summary>
/// Validates a <see cref="PagedRequest"/> against paging guardrails: page number lower bound and
/// page size bounds. Because <see cref="IValidator{T}"/> is contravariant, this validator also
/// validates subclasses of <see cref="PagedRequest"/>.
/// </summary>
public sealed class PagedRequestValidator : IValidator<PagedRequest>
{
    /// <inheritdoc />
    public ValidationErrors Validate(PagedRequest input)
    {
        var errors = ValidationErrors.Create();
        errors.AddIfError(() => input.PageNumber < 1, "PageNumber", "PageNumber must be greater than or equal to 1.");
        errors.AddIfError(() => input.PageSize < 1, "PageSize", "PageSize must be greater than or equal to 1.");
        errors.AddIfError(
            () => input.PageSize > PagedRequest.MaxPageSize,
            "PageSize",
            $"PageSize must not exceed {PagedRequest.MaxPageSize}.");

        return errors;
    }
}
