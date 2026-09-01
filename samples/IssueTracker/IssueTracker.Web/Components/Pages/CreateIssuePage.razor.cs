using D20Tek.Vertically;
using IssueTracker.Application.Features.Issues;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace IssueTracker.Web.Components.Pages;

public partial class CreateIssuePage
{
    private readonly CreateIssueModel _model = new();
    private EditContext _editContext = default!;
    private ValidationMessageStore _messageStore = default!;

    private bool _submitting;
    private string? _errorMessage;

    [Inject]
    private ICommandHandler<CreateIssue.Command, IssueResponse> CreateIssueHandler { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(_model);
        _messageStore = new ValidationMessageStore(_editContext);
    }

    private async Task SubmitAsync()
    {
        _submitting = true;
        _errorMessage = null;
        _messageStore.Clear();
        _editContext.NotifyValidationStateChanged();

        try
        {
            var view = (await CreateIssueHandler.HandleAsync(_model.ToCommand())).ToView();

            switch (view.Outcome)
            {
                case ResultViewOutcome.Success:
                    Navigation.NavigateTo($"/issues/{view.Value!.Id}");
                    return;

                case ResultViewOutcome.Validation:
                    ApplyFieldErrors(view.FieldErrors);
                    break;

                default:
                    _errorMessage = view.Message;
                    break;
            }
        }
        finally
        {
            _submitting = false;
        }
    }

    private void ApplyFieldErrors(IReadOnlyDictionary<string, string[]> fieldErrors)
    {
        foreach (var (field, messages) in fieldErrors)
        {
            _messageStore.Add(_editContext.Field(field), messages);
        }

        _editContext.NotifyValidationStateChanged();
    }

    private void Cancel() => Navigation.NavigateTo("/issues");
}
