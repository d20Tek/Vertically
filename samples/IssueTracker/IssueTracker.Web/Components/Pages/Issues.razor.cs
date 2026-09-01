using D20Tek.Vertically;
using D20Tek.Vertically.Queries.Pagination;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Features.Issues;
using IssueTracker.Application.Features.Users;
using Microsoft.AspNetCore.Components;

namespace IssueTracker.Web.Components.Pages;

public partial class Issues
{
    private IReadOnlyList<UserResponse> _users = [];
    private ResultView<PageOf<IssueResponse>>? _view;
    private PageOf<IssueResponse>? _page;

    private readonly IssueFilterCriteria _criteria = new();
    private int _pageNumber = 1;

    [Inject]
    private IQueryHandler<GetIssues.Query, PageOf<IssueResponse>> GetIssuesHandler { get; set; } = default!;

    [Inject]
    private IQueryHandler<GetUsers.Query, IReadOnlyList<UserResponse>> GetUsersHandler { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await LoadUsersAsync();
        await LoadPageAsync();
    }

    private async Task LoadUsersAsync()
    {
        var result = await GetUsersHandler.HandleAsync(new GetUsers.Query());
        var view = result.ToView();
        _users = view.IsSuccess ? view.Value! : [];
    }

    private async Task LoadPageAsync()
    {
        var query = new GetIssues.Query
        {
            PageNumber = _pageNumber,
            PageSize = _criteria.PageSize,
            Sorts = [new SortExpression(nameof(Issue.CreatedUtc), _criteria.SortDirection)],
            Filter = _criteria.ToFilter(),
        };

        _view = (await GetIssuesHandler.HandleAsync(query)).ToView();
        _page = _view.IsSuccess ? _view.Value : null;
    }

    private async Task ApplyFiltersAsync()
    {
        _pageNumber = 1;
        await LoadPageAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (_page is { HasPrevious: true })
        {
            _pageNumber--;
            await LoadPageAsync();
        }
    }

    private async Task NextPageAsync()
    {
        if (_page is { HasNext: true })
        {
            _pageNumber++;
            await LoadPageAsync();
        }
    }

    private string AssigneeName(Guid? assigneeId) =>
        assigneeId is null
            ? "Unassigned"
            : _users.FirstOrDefault(u => u.Id == assigneeId)?.FullName ?? "Unknown";

    private static string StatusClass(IssueStatus status) => status switch
    {
        IssueStatus.Open => "is-open",
        IssueStatus.InProgress => "is-inprogress",
        IssueStatus.Resolved => "is-resolved",
        IssueStatus.Closed => "is-closed",
        _ => string.Empty,
    };

    private static string PriorityClass(IssuePriority priority) => priority switch
    {
        IssuePriority.Low => "is-low",
        IssuePriority.Medium => "is-medium",
        IssuePriority.High => "is-high",
        IssuePriority.Critical => "is-critical",
        _ => string.Empty,
    };
}
