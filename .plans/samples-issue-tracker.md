# Issue Tracker Samples — Creation Plan

> Status: planning only (no code yet)
> Scope of this plan: shared **Application** + **Persistence** libraries and the **`IssueTracker.Api`** host (first host).
> Follow-up plans: `IssueTracker.Web` (Blazor Server) and `IssueTracker.Cli` hosts.

## Goal
Build a set of samples for **D20Tek.Vertically** around a single **Issue Tracker** scenario. All vertical
slices live in one UI-agnostic application library and are consumed unchanged by three different hosts
(WebApi, Blazor Server, CLI), proving the library's "write slices once, host them anywhere" promise.
Persistence is SQLite via EF Core, with the `DbContext` used directly inside handlers (through an
abstraction) for a realistic implementation.

## Scenario
A lightweight **Issue Tracker**: issues with title, description, status, priority, assignee, and
timestamps. Operations cover create / assign / change-status / get-by-id / paged-list, giving natural
homes for validation, `Result<T>` business-rule failures, and the new pagination query types.

## Project Layout
All sample projects live under `Samples/IssueTracker/`:

```
Samples/
  IssueTracker/
	IssueTracker.Application/     # UI- and provider-agnostic slices + domain + IIssueDbContext
	IssueTracker.Persistence/     # EF Core SQLite: IssueDbContext, config, migrations, seed, composition
	IssueTracker.Api/             # Host #1 — Minimal API (this plan)
	IssueTracker.Web/             # Host #2 — Blazor Server (later plan)
	IssueTracker.Cli/             # Host #3 — CLI (later plan)
```

## Architecture & Dependency Direction
```
IssueTracker.Application  ← IssueTracker.Persistence  ← Hosts (Api / Web / Cli)
		│                          │
		│ references only          │ references EFCore.Sqlite + Application
		│ Microsoft.EntityFrameworkCore (for DbSet<> on the interface)
		▼
   IIssueDbContext (abstraction used directly by handlers)
```

- **`IssueTracker.Application`** references the `D20Tek.Vertically` project and only
  `Microsoft.EntityFrameworkCore` (so it can expose `DbSet<Issue>` on the `IIssueDbContext` interface).
  It does **not** reference the SQLite provider — keeping the dependency direction clean.
- **`IssueTracker.Persistence`** references `IssueTracker.Application` and
  `Microsoft.EntityFrameworkCore.Sqlite`. It implements `IssueDbContext : DbContext, IIssueDbContext`,
  owns entity configuration, migrations, and seed data, and hosts the `AddIssueTracker(connectionString)`
  composition helper.
- **Hosts** reference `IssueTracker.Application` (to resolve/dispatch slices) and
  `IssueTracker.Persistence` (to call `AddIssueTracker`). They all point at the **same** repo-relative
  `issues.db` for genuine single-source sharing.

## Database Entity Model

The schema uses one aggregate table (`Issues`) plus two **lookup/reference tables**
(`IssueStatuses`, `IssuePriorities`) seeded with the basic values. Code-side enums
(`IssueStatus`, `IssuePriority`) mirror the lookup-table primary keys so the **rich domain** can
enforce rules using strongly-typed values while the database stores a **foreign key** to the
reference data (which also carries display names/sort order for UIs).

### `Issues` (aggregate table)
| Property | .NET type | DB column | Constraints / notes |
|---|---|---|---|
| `Id` | `Guid` | TEXT | **PK**; app-generated `Guid.CreateVersion7()` (sortable) |
| `Key` | `string` | TEXT | Friendly ref e.g. `ISSUE-1024`; **unique index**; auto-generated on create, user may set/change |
| `Title` | `string` | TEXT | Required; max length 200 |
| `Description` | `string?` | TEXT | Optional; max length 4000 |
| `StatusId` | `IssueStatus` | INTEGER | **FK → `IssueStatuses.Id`**; enum value stored as int; new issues start `Open` (set by the `Issue.Create` factory, not a DB default) |
| `PriorityId` | `IssuePriority` | INTEGER | **FK → `IssuePriorities.Id`**; enum value stored as int; **required** on create (no DB/app default) |
| `AssigneeId` | `Guid?` | TEXT | **FK → `Users.Id`**; nullable = unassigned |
| `CreatedUtc` | `DateTimeOffset` | TEXT | Set on insert; used for default `ORDER BY` (SQLite stores as ISO-8601 TEXT, sortable) |
| `UpdatedUtc` | `DateTimeOffset` | TEXT | Set on every mutation |

### `Users` table
| Column | .NET type | DB column | Constraints / notes |
|---|---|---|---|
| `Id` | `Guid` | TEXT | **PK**; app-generated `Guid.CreateVersion7()` |
| `FirstName` | `string` | TEXT | Required; max length 100 |
| `LastName` | `string` | TEXT | Required; max length 100 |
| `Email` | `string` | TEXT | Required; **unique index**; max length 256 |
| `CreatedUtc` | `DateTimeOffset` | TEXT | Set on insert |
| `UpdatedUtc` | `DateTimeOffset` | TEXT | Set on every mutation |

Referenced by `Issues.AssigneeId` via a FK constraint with **`ON DELETE SET NULL`** — deleting a user
unassigns (nulls `AssigneeId` on) their issues rather than blocking the delete. The full list is
retrievable (via a `GetUsers` query) to populate UI selectors for
`Issue.Assignee`, and seeded with a fixed set of users so all hosts start identically.

### `IssueStatuses` (lookup table)
| Column | Type | Notes |
|---|---|---|
| `Id` | INTEGER | **PK**; matches `IssueStatus` enum value |
| `Name` | TEXT | Display name; unique; e.g. `Open`, `InProgress`, `Resolved`, `Closed` |
| `SortOrder` | INTEGER | Stable ordering for UI dropdowns/boards |

Seed: `1=Open`, `2=InProgress`, `3=Resolved`, `4=Closed`.

### `IssuePriorities` (lookup table)
| Column | Type | Notes |
|---|---|---|
| `Id` | INTEGER | **PK**; matches `IssuePriority` enum value |
| `Name` | TEXT | Display name; unique; e.g. `Low`, `Medium`, `High`, `Critical` |
| `SortOrder` | INTEGER | Stable ordering for UI dropdowns |

Seed: `1=Low`, `2=Medium`, `3=High`, `4=Critical`.

### Code-side enums (domain vocabulary, values == lookup PKs)
- `IssueStatus` : `Open=1`, `InProgress=2`, `Resolved=3`, `Closed=4`.
- `IssuePriority` : `Low=1`, `Medium=2`, `High=3`, `Critical=4`.

The aggregate exposes these enums to callers/handlers; EF maps them to the `StatusId`/`PriorityId`
FK columns. The lookup tables exist for referential integrity, human-readable inspection of the
`.db` file, display names, and easy extension of the reference data.

### Rich domain (encapsulated `Issue` aggregate)
- Private setters; constructed via a factory (`Issue.Create(...)`) that generates `Id`/`Key`, stamps
  `CreatedUtc`, and returns `Result<Issue>` on invalid input.
- Behavior methods enforce rules and return `Result`:
  - `Assign(userId)` — sets `AssigneeId`; fails if `Status == Closed`. `Unassign()` clears it.
  - `ChangeStatus(target)` — enforces legal transitions; fails on illegal moves.
  - `Rename(title)` / `Describe(description)` / `Reprioritize(priority)` / `ChangeKey(key)` as needed,
    each stamping `UpdatedUtc`.
- **Status transition rules:**
  - `Open → InProgress → Resolved → Closed`
  - `Resolved → InProgress` (reopen for rework)
  - `Closed` is terminal (any change → `Result` failure)
  - Assignment blocked when `Closed`.

### Key generation & uniqueness
- On create, auto-generate `Key` as `ISSUE-{n}` using a simple monotonic counter. **SQLite does not
  support EF Core database sequences (`HasSequence`)**, so generation uses a persistent counter row
  (`Counters` table) incremented atomically per reservation.
- `Key` has a **unique index**; user-supplied or changed keys are validated for uniqueness (validator +
  DB constraint), returning a `Result` conflict failure on collision.

### Timestamps
- `DateTimeOffset` for `CreatedUtc`/`UpdatedUtc`. **SQLite cannot `ORDER BY` a `DateTimeOffset` column**,
  so the context applies a `DateTimeOffsetToBinaryConverter` (via `ConfigureConventions`) that persists
  them as a chronologically sortable `INTEGER`. This makes `ORDER BY CreatedUtc` (default sort for the
  paged query) translate server-side.

## Application Library — Contents
- **Domain**
  - `Issue` encapsulated aggregate: `Id`, `Key`, `Title`, `Description`, `Status` (`StatusId`),
	`Priority` (`PriorityId`), `AssigneeId`, `CreatedUtc`, `UpdatedUtc` — with factory + behavior methods.
  - `IssueStatus` enum (`Open=1`..`Closed=4`) and `IssuePriority` enum (`Low=1`..`Critical=4`) — values
	match the lookup-table PKs.
  - `IssueStatusRef` / `IssuePriorityRef` lookup entities (`Id`, `Name`, `SortOrder`) for the reference tables.
  - `User` entity (`Id`, `FirstName`, `LastName`, `Email`, `CreatedUtc`, `UpdatedUtc`) — referenced by
	`Issue.AssigneeId`; retrievable for UI assignee selectors.
- **Persistence abstraction**
  - `IIssueDbContext` — exposes `DbSet<Issue> Issues`, `DbSet<User> Users`, the lookup
	`DbSet<IssueStatusRef> IssueStatuses` and `DbSet<IssuePriorityRef> IssuePriorities`, and
	`SaveChangesAsync(CancellationToken)`. Handlers depend on this interface, never on the concrete
	context or provider.
- **Features** (each an `IFeature` grouping request + handler + validator + DTOs):
  - `CreateIssue` — command; validates title/priority; inserts a new `Issue`; returns new id / summary.
  - `AssignIssue` — command; sets assignee; `Result` failure if issue not found or already closed.
  - `ChangeIssueStatus` — command; enforces legal status transitions; `Result` failure on illegal moves.
  - `AssignIssue` — command; validates the target user exists; sets `AssigneeId`; `Result` failure if
	issue/user not found or issue already closed.
  - `GetIssueById` — query; returns a single issue detail DTO or a not-found `Result`.
  - `GetIssues` — query using `SortedFilteredPagedRequest` (filter by status/priority/assignee, sort),
	returning `PageOf<IssueSummary>`.
  - `GetUsers` — query; returns the list of users (`UserResponse`) for UI assignee selectors.
- **DTOs**: `IssueResponse`, `IssueSummary`, `UserResponse` (kept host-agnostic).

## Persistence Library — Contents
- `IssueDbContext : DbContext, IIssueDbContext` — implements the interface; `DbSet<Issue> Issues`,
  `DbSet<User> Users`, plus the `IssueStatuses` / `IssuePriorities` lookup sets.
- Entity configuration (keys, max lengths, unique index on `Key`, unique index on `User.Email`, FK
  relationships from `Issue` to the lookup tables and to `Users` (`AssigneeId`, **`OnDelete(SetNull)`**),
  non-unique indexes on `StatusId`/`PriorityId`/`AssigneeId`, enum-to-FK mapping).
- **Issue key generation (Option B — SQLite counter table):** a persistent `Counters` table (name/value)
  backs a `Task<long> NextIssueKeyNumberAsync(CancellationToken)` on `IIssueDbContext`, implemented in
  `IssueDbContext` by loading (or creating) the `issue-key` counter row, incrementing it, and saving.
  The `CreateIssue` handler formats the result as `ISSUE-{n}`. This replaces the interim random-key +
  retry-loop, and is used instead of a DB sequence because the SQLite provider does not support `HasSequence`.
- Deterministic **seed data**: the two lookup tables (statuses, priorities), a fixed set of users, plus a
  fixed set of issues (some assigned to seeded users) so all hosts start identically and pagination is
  demoable.
- **EF Core migration** (initial) — applied at startup via `Database.Migrate()`.
- `AddIssueTracker(this IServiceCollection, string connectionString)` composition helper that:
  1. registers `IssueDbContext` with the SQLite provider,
  2. maps `IIssueDbContext` → `IssueDbContext` (scoped),
  3. calls `AddVertically(b => b.Handlers.RegisterFromAssembly(typeof(<AppMarker>).Assembly); b.Behaviors.AddLogging()/AddValidation()/...)`,
  4. migrates and seeds the database on startup.

## Api Host (`IssueTracker.Api`) — First Host
- Minimal API host referencing Application + Persistence.
- `AddIssueTracker(<shared connection string>)` in composition root.
- Endpoints (grouped per-slice via a small `MapIssueEndpoints()` extension), each resolving the relevant
  handler and translating `Result<T>` → HTTP:
  - `POST /issues` → `CreateIssue`
  - `GET  /issues/{id}` → `GetIssueById`
  - `GET  /issues` → `GetIssues` (query string → `SortedFilteredPagedRequest`)
  - `POST /issues/{id}/assign` → `AssignIssue`
  - `POST /issues/{id}/status` → `ChangeIssueStatus`
  - `GET  /users` → `GetUsers` (assignee selector source)
- `Result<T>` translation: success → `200/201`; validation errors → `400` (problem details);
  not-found → `404`; business-rule failure → `409`/`422` as appropriate.

## Key Decisions (locked)
- **Shared scenario**, single Application library, three thin hosts.
- **Blazor Server** (not WASM) so the Web host can share the SQLite file directly.
- **SQLite + EF Core**, `DbContext` used directly in handlers via `IIssueDbContext` (no repository).
- **Separate `IssueTracker.Persistence`** project; interface (`IIssueDbContext`) defined in Application,
  implemented in Persistence — keeps dependency direction Application ← Persistence.
- **Pagination** demonstrated once via `GetIssues` using `SortedFilteredPagedRequest` → `PageOf<T>`.
- **Reference data as lookup tables** (`IssueStatuses`, `IssuePriorities`) seeded with basic values;
  code-side enums (`IssueStatus`/`IssuePriority`) mirror the lookup PKs and are stored as FK columns.
- **Rich (encapsulated) `Issue` aggregate** with a factory + behavior methods enforcing the status
  transition rules and returning `Result`.
- **Friendly `Key`** (`ISSUE-n`) auto-generated on create, user-settable/changeable, with a unique index.
  Generation uses a **persistent counter table** (Option B, SQLite-compatible) surfaced through `IIssueDbContext` for collision-free,
  monotonic numbers.
- **`Users` table** referenced by `Issues.AssigneeId` (nullable FK) with **`ON DELETE SET NULL`** —
  deleting a user unassigns their issues. Retrievable via `GetUsers` for UI assignee selectors.
- **`DateTimeOffset`** timestamps (SQLite ISO-8601 TEXT, sortable) with default paged sort on `CreatedUtc`.
- Central package management for EF Core versions; all projects inherit `net10.0` + warnings-as-errors.

## Open Defaults (change if desired)
- **Endpoint style:** grouped per-slice `MapIssueEndpoints()` extension (vs inline in `Program.cs`).
- **Shared DB path:** fixed repo-relative file (e.g., `Samples/IssueTracker/.data/issues.db`) so all
  three hosts share one source (vs per-host local path).
- **Schema creation:** initial migration + `Database.Migrate()` (vs `EnsureCreated()`).

## Risks & Notes
- `TreatWarningsAsErrors=true` + inherited `GenerateDocumentationFile=true` will surface XML-doc/nullable
  warnings in samples; likely disable the doc-file per sample project.
- EF Core version must align with `net10.0` (use the 10.x line to match the MEDI 10.0.11 packages).
- `RegisterFromAssembly` must target the **Application** assembly (where features live), not Persistence.
- Samples build in CI but are not unit-tested by this plan.

## Steps
1. [x] Add EF Core (Sqlite + design) package versions to `Directory.Packages.props`.
2. [x] Create `Samples/IssueTracker/IssueTracker.Application` classlib referencing D20Tek.Vertically + Microsoft.EntityFrameworkCore.
3. [x] Add the `IssueStatus`/`IssuePriority` enums, the `IssueStatusRef`/`IssuePriorityRef` lookup entities, the `User` entity, and the encapsulated `Issue` aggregate (factory + behavior methods + transition rules).
4. [x] Add the `IIssueDbContext` interface (`Issues` + `Users` + lookup `DbSet`s + `SaveChangesAsync`) in Application.
5. [x] Implement the `CreateIssue` feature (command, validator, handler against `IIssueDbContext`, DTO; auto-generates unique `Key`). *Interim key used a random + retry loop; replaced by the counter-table `NextIssueKeyNumberAsync` in step 10.*
6. [x] Implement the `AssignIssue` (validates target `User` exists via `AssigneeId` FK) and `ChangeIssueStatus` command features with business-rule `Result` failures.
7. [x] Implement the `GetIssueById` query feature and the `GetUsers` query feature (assignee selector source).
8. [x] Implement the `GetIssues` paged/sorted/filtered query feature using `SortedFilteredPagedRequest` and `PageOf<T>`.
9. [x] Create `Samples/IssueTracker/IssueTracker.Persistence` classlib referencing IssueTracker.Application + EFCore.Sqlite.
10. [x] Implement `IssueDbContext : DbContext, IIssueDbContext` with entity configuration (unique `Key`, unique `User.Email`, lookup + `Users` FKs, indexes), a persistent `Counters` table + `NextIssueKeyNumberAsync` (Option B, SQLite-compatible key generation), and deterministic seed (lookup tables + users + issues).
11. [x] Add the initial EF Core migration and the `AddIssueTracker` composition helper (context + `IIssueDbContext` + `AddVertically` + migrate/seed).
12. [x] Create `Samples/IssueTracker/IssueTracker.Api` Minimal API host mapping each slice to an endpoint with `Result<T>`→HTTP translation.
13. [x] Register the samples projects in `d20tek-vertically.slnx` under a `/Samples/` folder.
14. [x] Build the solution and run the `IssueTracker.Api` host to validate endpoints and pagination end-to-end.

## Deferred (future plans)
- `IssueTracker.Web` — Blazor Server host: paged issue board, create/assign/status forms, shares `issues.db`. **Plan added below.**
- `IssueTracker.Cli` — CLI host: `issue add/list/assign/status` verbs, `--status/--priority/--page/--size` options.
- Documentation pass under `docs/` describing the shared-slice pattern across the three hosts.

---

# IssueTracker.Web Sample — Creation Plan (Blazor Server)

> Status: planning only (no code yet)
> Scope of this plan: the **`IssueTracker.Web`** host (host #2). Reuses the existing
> **Application** and **Persistence** libraries unchanged — no new slices, domain, or schema.

## Goal
Add a second host, **`IssueTracker.Web`**, that consumes the *same* vertical slices as the API through
the shared Application/Persistence libraries — proving the "write slices once, host them anywhere"
promise with an interactive UI. The Web host renders an issue board and CRUD-ish workflows entirely by
dispatching the existing features (`CreateIssue`, `AssignIssue`, `ChangeIssueStatus`, `GetIssueById`,
`GetIssues`, `GetUsers`) and translating `Result<T>` into UI state rather than HTTP responses.

## Why Blazor Server (locked earlier)
- **Shares the SQLite file directly** — the server-side render host runs in-process with EF Core, so it
  points at the *same* repo-relative `issues.db` via the `{SharedDataDir}` token, exactly like the API.
- No serialization boundary or separate API contract needed; components call handlers directly through DI,
  the same way the API endpoints do.
- Keeps the sample focused on the shared-slice pattern instead of WASM hosting/transport concerns.

## Architecture & Dependency Direction
```
IssueTracker.Application  ← IssueTracker.Persistence  ← IssueTracker.Web (Blazor Server)
```
- **`IssueTracker.Web`** references `IssueTracker.Application` (to dispatch slices) and
  `IssueTracker.Persistence` (to call `AddIssueTrackerPersistence`). It composes behaviors itself via
  `AddIssueTrackerApplication(behaviors => ...)`, exactly like the API host does — behavior policy is a
  host decision (the Web host may pick a different set than the API, e.g. logging + validation without
  exception-to-result, since it surfaces failures as UI messages rather than problem details).
- Points at the **same** shared `issues.db` (via `{SharedDataDir}` token + `SharedDataPath`).
- Reuses `MigrateIssueTrackerAsync()` for the startup migrate/seed convenience (sample-only).

## Composition Root (`Program.cs`)
Mirror the API host's split registration, adapted for Blazor Server:
- `builder.Services.AddRazorComponents().AddInteractiveServerComponents();`
- Shared connection string: `builder.Configuration.GetConnectionString("IssueTracker") ?? "Data Source={SharedDataDir}/issues.db"`.
- `builder.Services.AddIssueTrackerApplication(behaviors => behaviors.AddLogging().AddValidation());`
  (host-chosen behavior policy — no exception-to-result by default; revisit if desired).
- `builder.Services.AddIssueTrackerPersistence(connectionString);`
- `await app.Services.MigrateIssueTrackerAsync();` on startup (sample convenience; same NOTE as the API).
- `app.MapRazorComponents<App>().AddInteractiveServerRenderMode();`
- Standard Blazor middleware: `UseStaticFiles()`/`MapStaticAssets()`, `UseAntiforgery()`, error handling.
- Logging config mirrors the API: `appsettings.json` + `appsettings.Development.json` with the same
  category levels (`D20Tek.Vertically` at Information/Debug, EF command logging quieted to `Warning`).

## UI Surface (components → slices)
Each page/component dispatches an existing feature; no new Application code.
- **Issues board / list** (`/` or `/issues`) — dispatches `GetIssues` with paging/sort/filter controls
  (status, priority, assignee filters; sort by created date). Renders `PageOf<IssueResponse>` with paging
  UI. This is the primary showcase of the pagination query types in a UI.
- **Issue detail** (`/issues/{id}`) — dispatches `GetIssueById`; not-found `Result` → a friendly
  "not found" UI state.
- **Create issue** (dialog or `/issues/new`) — form bound to `CreateIssue.Command`; validation failures
  from the pipeline surface as inline field/summary errors; success navigates to the new issue / refreshes
  the board.
- **Assign issue** — assignee dropdown populated by `GetUsers`; dispatches `AssignIssue`; business-rule
  failures (e.g. issue closed) shown as a UI message.
- **Change status** — status control dispatches `ChangeIssueStatus`; illegal-transition `Result` failures
  shown as a UI message (reusing the domain's transition rules).

## Result<T> → UI Translation
The Web analog of the API's `ResultHttpExtensions`: a small helper that maps `Result<T>` to component
state instead of HTTP.
- Success → bind value / navigate / close dialog.
- `ValidationErrors` → per-field + summary messages in the form (ideally integrated with `EditForm`
  validation, e.g. a custom `ValidationMessageStore` bridge).
- Not-found → dedicated "not found" render state.
- Business-rule failure → non-blocking error/toast message.
- Consider a shared `IssueTracker.Web` component/service (`ResultPresenter` / extension methods) so all
  components translate results consistently.

## Shared UI Concerns
- Enum display: reuse the lookup reference data (status/priority names + sort order) for dropdowns and
  badges; keep the same enum-name serialization intent as the API for consistency.
- A simple layout with nav (Board / New Issue) and a consistent status/priority badge styling.
- Keep styling minimal (sample scope) — focus on demonstrating slice reuse, not visual polish.

## Key Decisions (locked / proposed)
- **Blazor Server**, interactive server render mode (locked earlier), in-process EF Core, shared
  `issues.db`.
- **Reuse Application/Persistence unchanged** — zero new slices, domain, DTOs, or schema; the whole point
  is host reuse.
- **Split registration** via `AddIssueTrackerApplication(behaviors => ...)` + `AddIssueTrackerPersistence(...)`,
  with the Web host owning its behavior policy (proposed: logging + validation; no exception-to-result).
- **Startup migrate/seed** via `MigrateIssueTrackerAsync()` (sample convenience) — same non-production NOTE.
- **`Result<T>` → UI** translation helper as the Web analog of `ResultHttpExtensions`.

## Open Defaults (change if desired)
- **Create/assign/status UX:** dialogs vs dedicated pages.
- **Validation integration:** bridge pipeline `ValidationErrors` into `EditForm`/`ValidationMessageStore`
  vs a simpler summary panel.
- **Behavior policy:** logging + validation only (proposed) vs matching the API's full set.
- **Board vs table:** simple paged table (proposed) vs a status-column kanban board.

## Risks & Notes
- **Blazor Server + EF Core DbContext lifetime:** components can outlive a scope; be deliberate about
  `IIssueDbContext`/`DbContext` scoping. Since handlers are resolved per-dispatch (scoped) this is usually
  fine, but avoid capturing a `DbContext` across renders. Consider `IDbContextFactory` only if a concrete
  lifetime problem shows up (note it, don't pre-optimize).
- **Concurrency with the API sharing one `issues.db`:** SQLite file locking (WAL mode) is generally fine
  for a sample, but simultaneous writes from Web + API could surface transient locks; acceptable for a
  demo, worth a note.
- **Antiforgery / static assets:** ensure the standard Blazor middleware order is correct.
- `TreatWarningsAsErrors=true` + inherited doc-file settings — likely disable `GenerateDocumentationFile`
  for the Web project like the other samples.
- Samples build in CI but are not unit-tested by this plan.

## Steps
1. [x] Create `Samples/IssueTracker/IssueTracker.Web` Blazor Server project (interactive server render mode) referencing `IssueTracker.Application` + `IssueTracker.Persistence`.
2. [x] Configure the composition root: `AddRazorComponents().AddInteractiveServerComponents()`, shared connection string, `AddIssueTrackerApplication(behaviors => ...)`, `AddIssueTrackerPersistence(...)`, and startup `MigrateIssueTrackerAsync()`.
3. [x] Add `appsettings.json` + `appsettings.Development.json` mirroring the API host's logging levels and the `{SharedDataDir}` connection string.
4. [x] Add a `Result<T>` → UI translation helper (Web analog of `ResultHttpExtensions`) for success/validation/not-found/business-rule states.
5. [x] Build the issues board page dispatching `GetIssues` with paging/sort/filter controls, rendering `PageOf<IssueResponse>`.
6. [ ] Build the issue detail page dispatching `GetIssueById` with a not-found UI state.
7. [ ] Build the create-issue form bound to `CreateIssue.Command` with pipeline validation surfaced inline.
8. [ ] Build the assign action (assignee dropdown from `GetUsers` → `AssignIssue`) with business-rule failure messaging.
9. [ ] Build the change-status action dispatching `ChangeIssueStatus` with illegal-transition messaging.
10. [ ] Add layout/nav and shared status/priority badge styling using the lookup reference data.
11. [ ] Register `IssueTracker.Web` in `d20tek-vertically.slnx` under `/samples/IssueTracker/`.
12. [ ] Build the solution and run the Web host to validate the board, paging, and each workflow end-to-end against the shared `issues.db`.

