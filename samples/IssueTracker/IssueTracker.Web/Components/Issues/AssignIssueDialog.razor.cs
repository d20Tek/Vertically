using D20Tek.Vertically;
using IssueTracker.Application.Features.Issues;
using IssueTracker.Application.Features.Users;
using Microsoft.AspNetCore.Components;

namespace IssueTracker.Web.Components.Issues;

public partial class AssignIssueDialog
{
    private bool _visible;
    private Guid _selectedAssigneeId;
    private bool _assigning;
    private string? _error;

    /// <summary>The issue being (re)assigned.</summary>
    [Parameter, EditorRequired]
    public IssueResponse Issue { get; set; } = default!;

    /// <summary>The users available in the assignee selector.</summary>
    [Parameter]
    public IReadOnlyList<UserResponse> Users { get; set; } = [];

    /// <summary>Raised with the updated issue after a successful assignment.</summary>
    [Parameter]
    public EventCallback<IssueResponse> OnAssigned { get; set; }

    [Inject]
    private ICommandHandler<AssignIssue.Command, IssueResponse> AssignIssueHandler { get; set; } = default!;

    /// <summary>Opens the dialog, seeding the selection with the issue's current assignee.</summary>
    public void Show()
    {
        _selectedAssigneeId = Issue.AssigneeId ?? Guid.Empty;
        _error = null;
        _visible = true;
        StateHasChanged();
    }

    private void Close()
    {
        _visible = false;
        _assigning = false;
    }

    private bool CanAssign => !_assigning && _selectedAssigneeId != Guid.Empty && _selectedAssigneeId != Issue.AssigneeId;

    private async Task AssignAsync()
    {
        if (!CanAssign) return;

        _assigning = true;
        _error = null;

        try
        {
            var view = (await AssignIssueHandler.HandleAsync(new AssignIssue.Command(Issue.Id, _selectedAssigneeId)))
                                                .ToView();
            if (view.Outcome == ResultViewOutcome.Success)
            {
                await OnAssigned.InvokeAsync(view.Value);
                Close();
            }
            else
            {
                _error = view.Message;
            }
        }
        finally
        {
            _assigning = false;
        }
    }
}
