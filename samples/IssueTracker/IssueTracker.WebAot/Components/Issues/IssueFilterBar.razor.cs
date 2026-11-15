using IssueTracker.Application.Features.Users;
using IssueTracker.WebAot.Components.Pages;
using Microsoft.AspNetCore.Components;

namespace IssueTracker.WebAot.Components.Issues;

public partial class IssueFilterBar
{
    /// <summary>The filter/sort selections edited by this bar (owned by the parent page).</summary>
    [Parameter, EditorRequired]
    public IssueFilterCriteria Criteria { get; set; } = default!;

    /// <summary>The users available in the assignee selector.</summary>
    [Parameter]
    public IReadOnlyList<UserResponse> Users { get; set; } = [];

    /// <summary>Raised after any selection changes so the parent can re-run the query.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private Task NotifyChangedAsync() => OnChanged.InvokeAsync();
}
