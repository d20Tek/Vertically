using IssueTracker.Persistence;
using IssueTracker.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

// Shared, repo-relative SQLite database so every host reads/writes the same data.
var connectionString = builder.Configuration.GetConnectionString("IssueTracker") ?? "Data Source={SharedDataDir}/issues.db";

// Handler registration lives in the Application layer; behavior policy is a host decision.
// The Web host surfaces failures as UI messages, so it enables logging + validation
// (no exception-to-result — components translate Result<T> into UI state directly).
builder.Services.AddIssueTrackerApplication(behaviors => behaviors
    .AddLogging()
    .AddValidation());
builder.Services.AddIssueTrackerPersistence(connectionString);

var app = builder.Build();

// Apply migrations (and deterministic seed) on startup — sample convenience.
// NOTE: Don't do this in production, use a proper migration strategy.
await app.Services.MigrateIssueTrackerAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();
