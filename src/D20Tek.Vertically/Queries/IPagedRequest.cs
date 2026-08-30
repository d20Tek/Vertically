namespace D20Tek.Vertically.Queries;

/// <summary>
/// Marker interface for all paging request strategies (offset-based today, cursor/keyset later).
/// Lets handlers, behaviors, and registration recognize that a request is paged without coupling
/// to a specific paging strategy.
/// </summary>
public interface IPagedRequest
{
}
