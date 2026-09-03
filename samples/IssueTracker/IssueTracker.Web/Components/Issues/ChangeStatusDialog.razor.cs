using D20Tek.Vertically;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Features.Issues;
using Microsoft.AspNetCore.Components;

namespace IssueTracker.Web.Components.Issues;

public partial class ChangeStatusDialog
{
    private bool _visible;
    private IssueStatus _selectedStatus;
    private bool _saving;
    private string? _error;

    /// <summary>The issue whose status is being changed.</summary>
    [Parameter, EditorRequired]
    public IssueResponse Issue { get; set; } = default!;

    /// <summary>Raised with the updated issue after a successful status change.</summary>
    [Parameter]
    public EventCallback<IssueResponse> OnStatusChanged { get; set; }

    [Inject]
    private ICommandHandler<ChangeIssueStatus.Command, IssueResponse> ChangeStatusHandler { get; set; } = default!;

    /// <summary>Opens the dialog, seeding the selection with the issue's current status.</summary>
    public void Show()
    {
        _selectedStatus = Issue.Status;
        _error = null;
        _visible = true;
        StateHasChanged();
    }

    private void Close()
    {
        _visible = false;
        _saving = false;
    }

    private bool CanSave => !_saving && _selectedStatus != Issue.Status;

    private async Task SaveAsync()
    {
        if (!CanSave) return;

        _saving = true;
        _error = null;

        try
        {
            var view = (await ChangeStatusHandler.HandleAsync(new ChangeIssueStatus.Command(Issue.Id, _selectedStatus)))
                                                 .ToView();
            if (view.Outcome == ResultViewOutcome.Success)
            {
                await OnStatusChanged.InvokeAsync(view.Value);
                Close();
            }
            else
            {
                _error = view.Message;
            }
        }
        finally
        {
            _saving = false;
        }
    }
}
