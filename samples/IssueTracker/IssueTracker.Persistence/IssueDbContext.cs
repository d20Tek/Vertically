using IssueTracker.Application.Domain;
using IssueTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IssueTracker.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IIssueDbContext"/> backed by SQLite. Owns the entity
/// configuration (keys, indexes, FK relationships, enum-to-FK mapping), the reference-data and
/// user/issue seed, and the persistent counter used to generate friendly issue keys.
/// </summary>
public sealed class IssueDbContext(DbContextOptions<IssueDbContext> options) : DbContext(options), IIssueDbContext
{
    internal const string IssueKeyCounter = "issue-key";

    public DbSet<Issue> Issues => Set<Issue>();

    public DbSet<User> Users => Set<User>();

    public DbSet<IssueStatusRef> IssueStatuses => Set<IssueStatusRef>();

    public DbSet<IssuePriorityRef> IssuePriorities => Set<IssuePriorityRef>();

    internal DbSet<Counter> Counters => Set<Counter>();

    /// <inheritdoc />
    public async Task<long> NextIssueKeyNumberAsync(CancellationToken cancellationToken = default)
    {
        var counter = await Counters.FirstOrDefaultAsync(c => c.Name == IssueKeyCounter, cancellationToken);
        if (counter is null)
        {
            counter = new Counter { Name = IssueKeyCounter, Value = 0 };
            Counters.Add(counter);
        }

        counter.Value++;
        await SaveChangesAsync(cancellationToken);
        return counter.Value;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new IssueStatusRefConfiguration())
                    .ApplyConfiguration(new IssuePriorityRefConfiguration())
                    .ApplyConfiguration(new UserConfiguration())
                    .ApplyConfiguration(new IssueConfiguration())
                    .ApplyConfiguration(new CounterConfiguration())
                    .ApplySeedData();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // SQLite cannot ORDER BY a DateTimeOffset column. Persisting via the binary converter stores a
        // chronologically sortable value so server-side sorting (e.g. default CreatedUtc sort) works.
        configurationBuilder.Properties<DateTimeOffset>()
                            .HaveConversion<DateTimeOffsetToBinaryConverter>();
    }
}
