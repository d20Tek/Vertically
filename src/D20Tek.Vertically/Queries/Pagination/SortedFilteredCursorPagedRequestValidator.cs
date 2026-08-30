namespace D20Tek.Vertically.Queries.Pagination;

/// <summary>
/// Validates a <see cref="SortedFilteredCursorPagedRequest"/>. Reuses the base cursor paging bound
/// checks and additionally rejects sort/filter expressions with empty <c>Field</c> values. Field
/// allow-lists are app-specific and stay out of the core validator.
/// </summary>
public sealed class SortedFilteredCursorPagedRequestValidator
    : IValidator<SortedFilteredCursorPagedRequest>
{
    private readonly CursorPagedRequestValidator _baseValidator = new();

    /// <inheritdoc />
    public ValidationErrors Validate(SortedFilteredCursorPagedRequest input)
    {
        var errors = _baseValidator.Validate(input);

        errors.AddIfError(
            () => input.Sorts.Any(s => string.IsNullOrWhiteSpace(s.Field)),
            "Sorts",
            "Sort expressions must specify a non-empty Field.");
        errors.AddIfError(
            () => input.Filter is not null && FilterNodeValidation.HasEmptyField(input.Filter),
            "Filter",
            "Filter expressions must specify a non-empty Field.");

        return errors;
    }
}
