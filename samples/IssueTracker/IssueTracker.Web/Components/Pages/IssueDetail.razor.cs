using D20Tek.Vertically;
using IssueTracker.Application.Features.Issues;
using IssueTracker.Application.Features.Users;
using IssueTracker.Web.Components.Issues;
using Microsoft.AspNetCore.Components;

namespace IssueTracker.Web.Components.Pages;

public partial class IssueDetail
{
    private ResultView<IssueResponse>? _view;
    private IssueResponse? _issue;
    private IReadOnlyList<UserResponse> _users = [];
    private AssignIssueDialog _assignDialog = default!;
    private ChangeStatusDialog _statusDialog = default!;
    private ChangePriorityDialog _priorityDialog = default!;

    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    private IQueryHandler<GetIssueById.Query, IssueResponse> GetIssueByIdHandler { get; set; } = default!;

    [Inject]
    private IQueryHandler<GetUsers.Query, IReadOnlyList<UserResponse>> GetUsersHandler { get; set; } = default!;

    private string AssigneeName =>
        _issue?.AssigneeId is null
            ? "Unassigned"
            : _users.FirstOrDefault(u => u.Id == _issue.AssigneeId)?.FullName ?? "Unknown";

    protected override async Task OnParametersSetAsync()
    {
        _view = (await GetIssueByIdHandler.HandleAsync(new GetIssueById.Query(Id))).ToView();
        _issue = _view.IsSuccess ? _view.Value : null;

        if (_issue is not null)
        {
            await LoadUsersAsync();
        }
    }

    private async Task LoadUsersAsync()
    {
        var view = (await GetUsersHandler.HandleAsync(new GetUsers.Query())).ToView();
        _users = view.IsSuccess ? view.Value! : [];
    }

    private void OnAssigned(IssueResponse updated) => _issue = updated;

    private void OnStatusChanged(IssueResponse updated) => _issue = updated;

    private void OnPriorityChanged(IssueResponse updated) => _issue = updated;
}
