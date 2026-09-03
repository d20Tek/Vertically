using D20Tek.Vertically;
using IssueTracker.Application.Features.Issues;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace IssueTracker.Web.Components.Pages;

public partial class EditIssuePage
{
    private readonly EditIssueDetailsModel _model = new();
    private EditContext _editContext = default!;
    private ValidationMessageStore _messageStore = default!;

    private ResultView<IssueResponse>? _view;
    private IssueResponse? _issue;
    private bool _submitting;
    private string? _errorMessage;

    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    private IQueryHandler<GetIssueById.Query, IssueResponse> GetIssueByIdHandler { get; set; } = default!;

    [Inject]
    private ICommandHandler<EditIssueDetails.Command, IssueResponse> EditIssueDetailsHandler { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private string BackUrl => $"/issues/{Id}";

    protected override async Task OnParametersSetAsync()
    {
        _view = (await GetIssueByIdHandler.HandleAsync(new GetIssueById.Query(Id))).ToView();
        _issue = _view.IsSuccess ? _view.Value : null;

        if (_issue is not null)
        {
            _model.LoadFrom(_issue);
            _editContext = new EditContext(_model);
            _messageStore = new ValidationMessageStore(_editContext);
        }
    }

    private async Task SubmitAsync()
    {
        if (_issue is null) return;

        _submitting = true;
        _errorMessage = null;
        _messageStore.Clear();
        _editContext.NotifyValidationStateChanged();

        try
        {
            var view = (await EditIssueDetailsHandler.HandleAsync(_model.ToCommand(_issue.Id))).ToView();

            switch (view.Outcome)
            {
                case ResultViewOutcome.Success:
                    Navigation.NavigateTo(BackUrl);
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

    private void Cancel() => Navigation.NavigateTo(BackUrl);
}
