namespace D20Tek.Vertically.Queries;

/// <summary>
/// The boolean operator used by a <see cref="FilterGroup"/> to combine its child nodes.
/// </summary>
public enum FilterLogic
{
    /// <summary>All child nodes must match.</summary>
    And = 0,

    /// <summary>At least one child node must match.</summary>
    Or = 1,
}
