namespace D20Tek.Vertically.Queries;

/// <summary>
/// Validates a <see cref="SortedFilteredPagedRequest"/>. Reuses the base paging bound checks and
/// additionally rejects sort/filter expressions with empty <c>Field</c> values. Field allow-lists
/// are app-specific and stay out of the core validator.
/// </summary>
public sealed class SortedFilteredPagedRequestValidator : IValidator<SortedFilteredPagedRequest>
{
    private readonly PagedRequestValidator _baseValidator = new();

    /// <inheritdoc />
    public ValidationErrors Validate(SortedFilteredPagedRequest input)
    {
        var errors = _baseValidator.Validate(input);

        errors.AddIfError(
            () => input.Sorts.Any(s => string.IsNullOrWhiteSpace(s.Field)),
            "Sorts",
            "Sort expressions must specify a non-empty Field.");
        errors.AddIfError(
            () => input.Filters.Any(f => string.IsNullOrWhiteSpace(f.Field)),
            "Filters",
            "Filter expressions must specify a non-empty Field.");

        return errors;
    }
}
