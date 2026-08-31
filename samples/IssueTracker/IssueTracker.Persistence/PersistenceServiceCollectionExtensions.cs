using D20Tek.Vertically.Registration;
using IssueTracker.Application.Features.Issues;
using IssueTracker.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IssueTracker.Persistence;

/// <summary>
/// Composition helpers for wiring the SQLite-backed <see cref="IssueDbContext"/> and its
/// <see cref="IIssueDbContext"/> abstraction into a host's service collection.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers the full Issue Tracker sample stack: the Vertically feature handlers/validators from
    /// the Application assembly (<c>AddVertically</c>) and the SQLite-backed persistence
    /// (<see cref="AddIssueTrackerPersistence"/>).
    /// </summary>
    public static IServiceCollection AddIssueTracker(this IServiceCollection services, string connectionString) =>
        services.AddVertically(builder => builder.Handlers.RegisterFromAssembly(typeof(CreateIssue).Assembly))
                .AddIssueTrackerPersistence(connectionString);

    /// <summary>
    /// Registers <see cref="IssueDbContext"/> against the given SQLite connection string and exposes
    /// it as <see cref="IIssueDbContext"/> for the Application layer's handlers. The connection string
    /// may use the <c>{SharedDataDir}</c> token (see <see cref="SharedDataPath"/>) so all sample hosts
    /// resolve the same physical <c>issues.db</c>.
    /// </summary>
    public static IServiceCollection AddIssueTrackerPersistence(this IServiceCollection services, string connectionString) =>
        services.AddDbContext<IssueDbContext>(
                    options => options.UseSqlite(SharedDataPath.ResolveConnectionString(connectionString)))
                .AddScoped<IIssueDbContext>(sp => sp.GetRequiredService<IssueDbContext>());

    /// <summary>
    /// Applies any pending migrations for <see cref="IssueDbContext"/> (creating the database and the
    /// deterministic seed data on first run). Call once at startup.
    /// </summary>
    public static async Task MigrateIssueTrackerAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        await services.CreateAsyncScope()
                      .ServiceProvider.GetRequiredService<IssueDbContext>()
                      .Database.MigrateAsync(cancellationToken);
}
