namespace D20Tek.Vertically.Queries.Pagination;

/// <summary>
/// Base type for a node in a provider-agnostic filter tree. A node is either a single
/// <see cref="FilterExpression"/> leaf or a <see cref="FilterGroup"/> that combines child nodes
/// with a <see cref="FilterLogic"/> operator, enabling arbitrarily nested AND/OR logic.
/// The hierarchy is closed: only the node types defined in this assembly are permitted, so
/// consumers can exhaustively translate every node kind.
/// </summary>
public abstract record FilterNode
{
    private protected FilterNode()
    {
    }
}
