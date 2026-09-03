using D20Tek.Vertically;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Features.Issues;
using Microsoft.AspNetCore.Components;

namespace IssueTracker.Web.Components.Issues;

public partial class ChangePriorityDialog
{
    private bool _visible;
    private IssuePriority _selectedPriority;
    private bool _saving;
    private string? _error;

    /// <summary>The issue whose priority is being changed.</summary>
    [Parameter, EditorRequired]
    public IssueResponse Issue { get; set; } = default!;

    /// <summary>Raised with the updated issue after a successful priority change.</summary>
    [Parameter]
    public EventCallback<IssueResponse> OnPriorityChanged { get; set; }

    [Inject]
    private ICommandHandler<ChangeIssuePriority.Command, IssueResponse> ChangePriorityHandler { get; set; } = default!;

    /// <summary>Opens the dialog, seeding the selection with the issue's current priority.</summary>
    public void Show()
    {
        _selectedPriority = Issue.Priority;
        _error = null;
        _visible = true;
        StateHasChanged();
    }

    private void Close()
    {
        _visible = false;
        _saving = false;
    }

    private bool CanSave => !_saving && _selectedPriority != Issue.Priority;

    private async Task SaveAsync()
    {
        if (!CanSave) return;

        _saving = true;
        _error = null;

        try
        {
            var view = (await ChangePriorityHandler.HandleAsync(new ChangeIssuePriority.Command(Issue.Id, _selectedPriority)))
                                                   .ToView();
            if (view.Outcome == ResultViewOutcome.Success)
            {
                await OnPriorityChanged.InvokeAsync(view.Value);
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
