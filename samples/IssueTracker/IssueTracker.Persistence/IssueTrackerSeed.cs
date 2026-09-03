using IssueTracker.Application.Domain;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Persistence;

/// <summary>
/// Deterministic seed data for the Issue Tracker sample: reference lookups, sample users, sample
/// issues, and the issue-key counter primed to match the seeded keys. All identifiers and timestamps
/// are fixed constants so migrations and demos are reproducible.
/// </summary>
internal static class IssueTrackerSeed
{
    private static readonly DateTimeOffset SeedUtc = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly Guid AdaId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BenId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ClaraId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PedroId = new("44444444-4444-4444-4444-444444444444");

    private static readonly Guid Issue1Id = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Issue2Id = new("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Issue3Id = new("aaaaaaaa-0000-0000-0000-000000000003");

    public static void ApplySeedData(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IssueStatusRef>().HasData(
            new IssueStatusRef { Id = IssueStatus.Open, Name = "Open", SortOrder = 1 },
            new IssueStatusRef { Id = IssueStatus.InProgress, Name = "In Progress", SortOrder = 2 },
            new IssueStatusRef { Id = IssueStatus.Resolved, Name = "Resolved", SortOrder = 3 },
            new IssueStatusRef { Id = IssueStatus.Closed, Name = "Closed", SortOrder = 4 });

        modelBuilder.Entity<IssuePriorityRef>().HasData(
            new IssuePriorityRef { Id = IssuePriority.Critical, Name = "Critical", SortOrder = 1 },
            new IssuePriorityRef { Id = IssuePriority.High, Name = "High", SortOrder = 2 },
            new IssuePriorityRef { Id = IssuePriority.Medium, Name = "Medium", SortOrder = 3 },
            new IssuePriorityRef { Id = IssuePriority.Low, Name = "Low", SortOrder = 4 });

        modelBuilder.Entity<User>().HasData(
            new { Id = AdaId, FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", CreatedUtc = SeedUtc, UpdatedUtc = SeedUtc },
            new { Id = BenId, FirstName = "Ben", LastName = "Turing", Email = "ben@example.com", CreatedUtc = SeedUtc, UpdatedUtc = SeedUtc },
            new { Id = ClaraId, FirstName = "Clara", LastName = "Hopper", Email = "clara@example.com", CreatedUtc = SeedUtc, UpdatedUtc = SeedUtc },
            new { Id = PedroId, FirstName = "Pedro", LastName = "Silva", Email = "pedro@example.com", CreatedUtc = SeedUtc, UpdatedUtc = SeedUtc });

        modelBuilder.Entity<Issue>().HasData(
            new
            {
                Id = Issue1Id,
                Key = "ISSUE-1",
                Title = "Login page throws on empty password",
                Description = (string?)"Submitting the login form with an empty password returns a 500.",
                Status = IssueStatus.Open,
                Priority = IssuePriority.Critical,
                AssigneeId = (Guid?)AdaId,
                CreatedUtc = SeedUtc,
                UpdatedUtc = SeedUtc,
            },
            new
            {
                Id = Issue2Id,
                Key = "ISSUE-2",
                Title = "Improve dashboard load time",
                Description = (string?)"The dashboard takes several seconds to render for large datasets.",
                Status = IssueStatus.InProgress,
                Priority = IssuePriority.High,
                AssigneeId = (Guid?)BenId,
                CreatedUtc = SeedUtc,
                UpdatedUtc = SeedUtc,
            },
            new
            {
                Id = Issue3Id,
                Key = "ISSUE-3",
                Title = "Add dark mode toggle",
                Description = (string?)null,
                Status = IssueStatus.Open,
                Priority = IssuePriority.Low,
                AssigneeId = (Guid?)null,
                CreatedUtc = SeedUtc,
                UpdatedUtc = SeedUtc,
            });

        modelBuilder.Entity<Counter>().HasData(new Counter { Name = IssueDbContext.IssueKeyCounter, Value = 3 });
    }
}
