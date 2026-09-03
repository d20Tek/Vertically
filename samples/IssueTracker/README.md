# Issue Tracker - an end-to-end D20Tek.Vertically sample

A lightweight **Issue Tracker** built to demonstrate **vertical slice architecture** with
[D20Tek.Vertically](../../src/D20Tek.Vertically). The interesting part isn't the domain - it's that
**three different hosts (a REST API, a Blazor web app, and a CLI) all consume the exact same
Application and Persistence layers**. No business logic is duplicated per host; each host only owns its
own presentation and cross-cutting *policy*.

## Projects

| Project | Role |
| --- | --- |
| [`IssueTracker.Application`](IssueTracker.Application) | The core. Rich domain (`Issue` aggregate, `User`, status/priority policy) plus one **vertical slice per feature** (command/query + validator + handler), each a self-registering `IFeature`. Depends only on EF Core abstractions via `IIssueDbContext`. |
| [`IssueTracker.Persistence`](IssueTracker.Persistence) | EF Core (SQLite) implementation of `IIssueDbContext`, entity configuration, migrations, deterministic seed, and the shared-database resolution helper. |
| [`IssueTracker.Api`](IssueTracker.Api) | Minimal API host. Maps each slice to an endpoint and translates `Result<T>` → HTTP (RFC 7807 problem details). OpenAPI + Scalar UI. |
| [`IssueTracker.Web`](IssueTracker.Web) | Blazor Web App (interactive server). Dispatches the same slices from components and translates `Result<T>` → UI state. |
| [`IssueTracker.Cli`](IssueTracker.Cli) | Console host (System.CommandLine). Dispatches the same slices from verbs and translates `Result<T>` → stdout/stderr + exit codes. |

```
IssueTracker.Api ─┐
IssueTracker.Web ─┼─► IssueTracker.Application ─► IIssueDbContext ◄─ IssueTracker.Persistence
IssueTracker.Cli ─┘        (vertical slices)                              (EF Core / SQLite)
```

## The shared-slice pattern

Each feature lives in **one file** in the Application layer as an `IFeature` bundling its command/query,
validator, and handler - for example
[`CreateIssue`](IssueTracker.Application/Features/Issues/CreateIssue.cs),
[`GetIssues`](IssueTracker.Application/Features/Issues/GetIssues.cs), or
[`CreateUser`](IssueTracker.Application/Features/Users/CreateUser.cs). Handlers depend only on the
`IIssueDbContext` abstraction, so they are host- and provider-agnostic.

Every host composes the app the same way, splitting **registration** (owned by Application) from
**behavior policy** (owned by the host):

```csharp
// Handler registration is intrinsic to the Application layer...
builder.Services.AddIssueTrackerApplication(behaviors => behaviors
	// ...but the pipeline behaviors are a host decision.
	.AddExceptionToResult()
	.AddLogging()
	.AddValidation());

builder.Services.AddIssueTrackerPersistence(connectionString);
await app.Services.MigrateIssueTrackerAsync(); // sample convenience - migrate + seed on startup
```

`AddIssueTrackerApplication` calls `RegisterFromAssembly(...)`, which discovers **every** `IFeature`
automatically - adding a new slice needs no host changes. The `configureBehaviors` callback is where
each host expresses its own cross-cutting policy (see
[`ApplicationServiceCollectionExtensions`](IssueTracker.Application/ApplicationServiceCollectionExtensions.cs)).

Each host then dispatches handlers and translates the returned `Result<T>` into its own idiom:

| Host | Dispatch site | `Result<T>` translation |
| --- | --- | --- |
| API | endpoint delegates ([`IssueEndpoints`](IssueTracker.Api/IssueEndpoints.cs)) | HTTP status / RFC 7807 problem details |
| Web | Razor components (per-render DI scope) | inline UI success/error state |
| CLI | command verbs (per-invocation DI scope) | stdout on success, stderr + exit code on failure |

## The shared `issues.db` story

All three hosts read and write the **same physical SQLite file** so you can, say, create an issue from
the CLI and immediately see it in the web app. Each host's connection string uses a `{SharedDataDir}`
token:

```
"Data Source={SharedDataDir}/issues.db"
```

At runtime [`SharedDataPath`](IssueTracker.Persistence/SharedDataPath.cs) expands that token by walking
up from the host's output directory to the nearest `IssueTracker` folder (this `samples/IssueTracker`
directory) and rewrites `Data Source` to that absolute path. Without this, each host would get its own
copy of the database under its `bin` output. The database (and its `-wal`/`-shm` companions) is
git-ignored, and it is created, migrated, and seeded automatically on first run via
`MigrateIssueTrackerAsync()`.

> The startup migrate/seed is a **sample convenience** - don't auto-migrate in production; use a proper
> migration strategy.

## Running each host

From the repository root:

### API

```powershell
dotnet run --project samples/IssueTracker/IssueTracker.Api
```

Browse the interactive **Scalar** API reference at `/scalar`. Endpoints include:

- `POST /issues`, `GET /issues`, `GET /issues/{id}`
- `POST /issues/{id}/assign`, `POST /issues/{id}/status`
- `GET /users`

### Web

```powershell
dotnet run --project samples/IssueTracker/IssueTracker.Web
```

Open the printed URL for the issues board, with filtering/sorting/paging and create/assign/status/
priority/edit workflows.

### CLI

```powershell
dotnet run --project samples/IssueTracker/IssueTracker.Cli -- <command>
```

The command tree is grouped under `issue` and `user`:

```
issue list [--status] [--priority] [--assignee] [--sort] [--page] [--size]
issue show <key>
issue create --title <t> [--description <d>] [--priority <p>] [--key <k>]
issue assign <key> --user <userId>
issue status <key> --to <status>
issue priority <key> --to <priority>
issue edit <key> [--title <t>] [--description <d>]
user list
user create --first-name <f> --last-name <l> --email <e>
```

Issue-identifying verbs take the **friendly key** (e.g. `ISSUE-1`) rather than the Guid. Since assigning
still needs a user Guid, `user list` prints the full user ids for copy/paste. `--help` is available at
every level.

Example lifecycle:

```powershell
dotnet run --project samples/IssueTracker/IssueTracker.Cli -- issue create --title "Login fails" --priority High
dotnet run --project samples/IssueTracker/IssueTracker.Cli -- user list
dotnet run --project samples/IssueTracker/IssueTracker.Cli -- issue assign ISSUE-6 --user <userId>
dotnet run --project samples/IssueTracker/IssueTracker.Cli -- issue status ISSUE-6 --to InProgress
dotnet run --project samples/IssueTracker/IssueTracker.Cli -- issue show ISSUE-6
```

The CLI maps `Result<T>` outcomes to distinct **exit codes** so it composes in scripts:

| Exit code | Meaning |
| --- | --- |
| `0` | Success |
| `1` | Unspecified failure |
| `2` | Validation / invalid input |
| `3` | Conflict (e.g. duplicate issue key or user email) |
| `4` | Not found |

## Key takeaways

- **Write a feature once, reach it from anywhere.** The slice, its validation, and its business rules
  are defined a single time in the Application layer and reused verbatim by all three hosts.
- **Separation of registration and policy.** Handler registration belongs to the Application layer;
  pipeline behaviors (logging, validation, exception-to-result) are chosen per host.
- **Presentation is the only per-host code.** Each host differs only in how it accepts input and
  translates `Result<T>` - HTTP, UI, or exit codes.
