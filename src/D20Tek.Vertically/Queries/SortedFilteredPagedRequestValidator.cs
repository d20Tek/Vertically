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
            () => input.Filter is not null && HasEmptyField(input.Filter),
            "Filter",
            "Filter expressions must specify a non-empty Field.");

        return errors;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static bool HasEmptyField(FilterNode node) =>
        node switch
        {
            FilterExpression expression => string.IsNullOrWhiteSpace(expression.Field),
            FilterGroup group => group.Nodes.Any(HasEmptyField),
            _ => throw new UnreachableException($"Unhandled filter node type: {node.GetType().Name}."),
        };
}
