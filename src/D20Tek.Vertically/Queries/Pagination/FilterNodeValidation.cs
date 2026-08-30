namespace D20Tek.Vertically.Queries.Pagination;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Shared validation helpers for provider-agnostic filter trees.
/// </summary>
internal static class FilterNodeValidation
{
    /// <summary>
    /// Returns <c>true</c> when any leaf expression in the tree has a null or whitespace field.
    /// </summary>
    /// <param name="node">The root node to inspect.</param>
    [ExcludeFromCodeCoverage]
    internal static bool HasEmptyField(FilterNode node) =>
        node switch
        {
            FilterExpression expression => string.IsNullOrWhiteSpace(expression.Field),
            FilterGroup group => group.Nodes.Any(HasEmptyField),
            _ => throw new UnreachableException($"Unhandled filter node type: {node.GetType().Name}."),
        };
}
