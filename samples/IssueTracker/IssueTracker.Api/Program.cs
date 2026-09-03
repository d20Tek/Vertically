using System.Text.Json.Serialization;
using IssueTracker.Api;
using IssueTracker.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Serialize enums (IssueStatus/IssuePriority) as their names for a readable, stable API surface.
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Shared, repo-relative SQLite database so every host reads/writes the same data.
var connectionString = builder.Configuration.GetConnectionString("IssueTracker") ?? "Data Source={SharedDataDir}/issues.db";

// Handler registration lives in the Application layer; behavior policy is a host decision.
// This API surfaces failures as RFC 7807 problem details, so exception-to-result is enabled.
builder.Services.AddIssueTrackerApplication(behaviors => behaviors
    .AddExceptionToResult()
    .AddLogging()
    .AddValidation());
builder.Services.AddIssueTrackerPersistence(connectionString);

var app = builder.Build();

// Apply migrations (and deterministic seed) on startup — sample convenience.
// NOTE: Don't do this in production, use a proper migration strategy.
await app.Services.MigrateIssueTrackerAsync();

// Expose the OpenAPI document and an interactive Scalar API reference UI (at /scalar).
app.MapOpenApi();
app.MapScalarApiReference();

app.MapIssueEndpoints();
app.MapUserEndpoints();

app.Run();
