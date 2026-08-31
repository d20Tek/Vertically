using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IssueTracker.Persistence;

/// <summary>
/// Design-time factory used by the EF Core tools (e.g. <c>dotnet ef migrations add</c>) to construct
/// an <see cref="IssueDbContext"/> without a running host. Uses a local SQLite file so the tools can
/// build the model; the runtime host supplies its own connection string via DI.
/// </summary>
internal sealed class IssueDbContextFactory : IDesignTimeDbContextFactory<IssueDbContext>
{
    public IssueDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IssueDbContext>()
            .UseSqlite("Data Source=issues.db")
            .Options;

        return new IssueDbContext(options);
    }
}
