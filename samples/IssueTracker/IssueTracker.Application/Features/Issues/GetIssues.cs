using D20Tek.Functional;
using D20Tek.Vertically;
using D20Tek.Vertically.Queries.Pagination;
using D20Tek.Vertically.Registration;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Application.Features.Issues;

/// <summary>
/// Vertical slice that returns a paged, sorted, and filtered list of <see cref="Issue"/>s as a
/// <see cref="PageOf{T}"/> of <see cref="IssueResponse"/>. Demonstrates the library's offset paging
/// with provider-agnostic sort/filter trees resolved server-side via <see cref="IssueQueryTranslator"/>.
/// </summary>
public sealed class GetIssues : IFeature
{
    public void Register(IVerticallyBuilder builder) =>
        builder.Handlers.AddQueryHandler<Handler>()
                        .AddValidator<Validator>();

    /// <summary>Paged/sorted/filtered request for issues.</summary>
    public sealed record Query : SortedFilteredPagedRequest, IQuery<PageOf<IssueResponse>>;

    /// <summary>
    /// Validates the paging bounds and rejects sort/filter fields that are not part of the issue
    /// allow-list, layering app-specific rules on top of the base paged-request checks.
    /// </summary>
    public sealed class Validator : IValidator<Query>
    {
        private readonly SortedFilteredPagedRequestValidator _baseValidator = new();

        public ValidationErrors Validate(Query input)
        {
            var errors = _baseValidator.Validate(input);

            errors.AddIfError(
                () => input.Sorts.Any(s => !IssueQueryTranslator.IsKnownField(s.Field)),
                nameof(Query.Sorts),
                "One or more sort fields are not supported for issues.");
            errors.AddIfError(
                () => input.Filter is not null && HasUnknownFilterField(input.Filter),
                nameof(Query.Filter),
                "One or more filter fields are not supported for issues.");

            return errors;
        }

        private static bool HasUnknownFilterField(FilterNode node) =>
            node switch
            {
                FilterGroup group => group.Nodes.Any(HasUnknownFilterField),
                FilterExpression expression => !IssueQueryTranslator.IsKnownField(expression.Field),
                _ => true,
            };
    }

    /// <summary>Applies the filter, sort, and page window server-side and projects to responses.</summary>
    public sealed class Handler(IIssueDbContext dbContext) : IQueryHandler<Query, PageOf<IssueResponse>>
    {
        private readonly IIssueDbContext _dbContext = dbContext;

        public async Task<Result<PageOf<IssueResponse>>> HandleAsync(Query query, CancellationToken cancellationToken = default)
        {
            var filtered = IssueQueryTranslator.ApplyFilter(_dbContext.Issues.AsNoTracking(), query.Filter);
            var totalCount = await filtered.LongCountAsync(cancellationToken);

            var items = await IssueQueryTranslator.ApplySort(filtered, query.Sorts)
                .Skip(query.Skip)
                .Take(query.Take)
                .Select(i => new IssueResponse(
                    i.Id,
                    i.Key,
                    i.Title,
                    i.Description,
                    i.Status,
                    i.Priority,
                    i.AssigneeId,
                    i.CreatedUtc,
                    i.UpdatedUtc))
                .ToListAsync(cancellationToken);

            var page = PageOf<IssueResponse>.Create(items, query, totalCount);
            return Result<PageOf<IssueResponse>>.Success(page);
        }
    }
}
