namespace D20Tek.Vertically.Queries.Pagination;

/// <summary>
/// A composite filter node that combines its <see cref="Nodes"/> with a boolean
/// <see cref="Logic"/> operator. Because each child is itself a <see cref="FilterNode"/>, groups can
/// nest to express arbitrary AND/OR filter trees.
/// </summary>
/// <param name="Logic">The boolean operator used to combine the child nodes.</param>
/// <param name="Nodes">The child nodes (leaf expressions or nested groups) to combine.</param>
public sealed record FilterGroup(FilterLogic Logic, IReadOnlyList<FilterNode> Nodes) : FilterNode
{
    /// <summary>Creates a group that requires all supplied nodes to match.</summary>
    /// <param name="nodes">The child nodes to combine with AND.</param>
    public static FilterGroup All(params FilterNode[] nodes) => new(FilterLogic.And, nodes);

    /// <summary>Creates a group that requires at least one supplied node to match.</summary>
    /// <param name="nodes">The child nodes to combine with OR.</param>
    public static FilterGroup Any(params FilterNode[] nodes) => new(FilterLogic.Or, nodes);
}
