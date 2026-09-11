# Future Features Backlog

This document accumulates candidate features and enhancements for future versions of D20Tek.Vertically.
Each entry should capture enough context (motivation, design sketch, and source references) to pick the
work up later without re-deriving it.

---

## 1. `D20Tek.Vertically.EntityFrameworkCore` — Generic Sorted/Filtered Query Translator

### Status
Proposed. Reference implementation already exists (inline) in the Issue Tracker sample at
`samples/IssueTracker/IssueTracker.Application/Features/Issues/IssueQueryTranslator.cs`.

### Motivation
The core library ships the provider-agnostic *description* types for querying
(`SortExpression`, `SortDirection`, `FilterNode`, `FilterGroup`, `FilterExpression`, `FilterOperator`,
`FilterLogic`) plus the paged request/response types (`PagedRequest`, `SortedFilteredPagedRequest`,
`PageOf<T>`). However, it does **not** ship an adapter that resolves those descriptions into an actual
`IQueryable<T>` for a concrete provider. Today every consumer must re-implement the same reflection /
expression-tree code to turn a `SortedFilteredPagedRequest` into `Where(...)` / `OrderBy(...)` calls.

Analysis of the sample translator showed ~90% of the code is entirely generic (entity-agnostic
expression building), and only ~10% is entity-specific (the field allow-list and the default sort).
Promoting the generic 90% into a companion EF Core package closes the loop so consumers only declare the
per-entity bits.

### Proposed package
- **Name:** `D20Tek.Vertically.EntityFrameworkCore` (companion package; keeps the core library free of an
  EF Core dependency).
- **Target frameworks:** match the core library (`net9.0`, `net10.0`).
- **Dependencies:** `D20Tek.Vertically` (core) + `Microsoft.EntityFrameworkCore` (or just
  `System.Linq.Queryable` if we can keep it provider-neutral — see Open Questions).

### Proposed public surface
A generic, entity-agnostic builder driven by a caller-supplied field allow-list:

```csharp
public static class QueryableFilterBuilder
{
	// Applies the filter tree (AND/OR nesting) to the query. No-op when filter is null/empty.
	public static IQueryable<T> ApplyFilter<T>(
		IQueryable<T> query,
		FilterGroup? filter,
		IReadOnlyDictionary<string, string> fieldMap);

	// Applies ordered sort instructions; uses defaultSort when sorts is empty.
	public static IQueryable<T> ApplySort<T>(
		IQueryable<T> query,
		IReadOnlyList<SortExpression> sorts,
		IReadOnlyDictionary<string, string> fieldMap,
		Expression<Func<T, object>>? defaultSort = null,
		SortDirection defaultDirection = SortDirection.Descending);

	// True when the field is present in the allow-list (and non-empty).
	public static bool IsKnownField(
		string field,
		IReadOnlyDictionary<string, string> fieldMap);

	// Recursively checks a filter tree for any field not in the allow-list (for validators).
	public static bool HasUnknownFilterField(
		FilterNode node,
		IReadOnlyDictionary<string, string> fieldMap);
}
```

Optional convenience extension to run the full pipeline and materialize a page:

```csharp
public static class QueryablePagingExtensions
{
	public static Task<PageOf<T>> ToPageAsync<T>(
		this IQueryable<T> query,
		SortedFilteredPagedRequest request,
		IReadOnlyDictionary<string, string> fieldMap,
		Expression<Func<T, object>>? defaultSort = null,
		CancellationToken cancellationToken = default);
	// Internally: ApplyFilter -> LongCountAsync -> ApplySort -> Skip/Take -> ToListAsync -> PageOf.Create.
	// NOTE: async count/materialization requires EF Core (ToListAsync/LongCountAsync), hence the EF package.
}
```

Per-entity usage then collapses to just the allow-list + default sort:

```csharp
internal static class IssueQueryTranslator
{
	private static readonly Dictionary<string, string> FieldMap = new(StringComparer.OrdinalIgnoreCase)
	{
		[nameof(Issue.Key)] = nameof(Issue.Key),
		// ... remaining allowed fields ...
	};

	public static IQueryable<Issue> ApplyFilter(IQueryable<Issue> q, FilterGroup? f)
		=> QueryableFilterBuilder.ApplyFilter(q, f, FieldMap);

	public static IQueryable<Issue> ApplySort(IQueryable<Issue> q, IReadOnlyList<SortExpression> s)
		=> QueryableFilterBuilder.ApplySort(q, s, FieldMap, defaultSort: i => i.CreatedUtc);

	public static bool IsKnownField(string field)
		=> QueryableFilterBuilder.IsKnownField(field, FieldMap);
}
```

### Implementation details to preserve (from the sample reference)
The reference implementation is the source of truth for the algorithm. Key mechanics to carry over:

- **Field allow-list**: `Dictionary<string,string>` (case-insensitive, `StringComparer.OrdinalIgnoreCase`)
  mapping request-field-name -> entity-member-name. Guards against sorting/filtering arbitrary members.
- **Filter building** (`ApplyFilter` / `BuildNode` / `BuildGroup` / `BuildExpression`):
  - Single `ParameterExpression` of type `T` reused across the tree.
  - `FilterGroup` -> fold children with `Expression.AndAlso` / `Expression.OrElse` based on `FilterLogic`.
  - Empty group -> `Expression.Constant(true)`.
  - `FilterExpression` -> `Expression.Property` + coerced `Expression.Constant`, then map `FilterOperator`:
	- `Equals/NotEquals/GreaterThan/GreaterThanOrEqual/LessThan/LessThanOrEqual` -> binary comparisons.
	- `Contains/StartsWith/EndsWith` -> `string` instance-method calls via reflected `MethodInfo`.
	- `In/NotIn` -> NOT yet implemented in the sample; add collection `Contains` support in the package.
  - Unsupported node/operator -> throw `NotSupportedException`.
- **Sort building** (`ApplySort` / `ApplyOrder`):
  - Empty sorts -> apply `defaultSort` (sample uses `OrderByDescending(i => i.CreatedUtc)`).
  - First sort -> `OrderBy`/`OrderByDescending`; subsequent -> `ThenBy`/`ThenByDescending`.
  - Uses reflected `Queryable` generic methods (`MakeGenericMethod(typeof(T), property.Type)`); switch
	must stay exhaustive (undefined enum -> treat as ascending via `_` fallthrough).
- **Value coercion** (`CoerceValue`):
  - Strip `Nullable<>` via `Nullable.GetUnderlyingType`.
  - `null` -> null; already-assignable value -> as-is.
  - `switch`: enum (`{ IsEnum: true }` -> `Enum.Parse` ignoreCase), `Guid` -> `Guid.Parse`,
	`DateTimeOffset` -> `DateTimeOffset.Parse` (InvariantCulture), else `Convert.ChangeType` (InvariantCulture).
- **Validator support**: expose `HasUnknownFilterField` so app validators can reject unknown sort/filter
  fields (the sample currently reimplements this recursion inline in `GetIssues.Validator`).

### Enhancements over the sample
- Add `In` / `NotIn` operator support (collection membership -> `Enumerable.Contains`).
- Consider case-insensitive string comparisons (provider-dependent; document collation caveats).
- Consider caching compiled member accessors / `MethodInfo` lookups for hot paths.
- Consider AOT/trim implications of the reflection over `Queryable` (may need `[RequiresUnreferencedCode]`
  annotations or a source-generated alternative).

### Open questions
- **Provider neutrality:** `ApplyFilter`/`ApplySort` are pure `IQueryable` and need no EF reference, but the
  `ToPageAsync` convenience needs `ToListAsync`/`LongCountAsync` (EF Core). Options: (a) put builder in a
  provider-neutral package and only the async paging extension in the EF package, or (b) ship both in the
  EF package for simplicity. Leaning toward (a) if the split stays clean.
- **Field mapping ergonomics:** dictionary vs. a small fluent `FieldMapBuilder<T>` with
  `Map(x => x.CreatedUtc)` to get compile-time member safety instead of `nameof` strings.
- **Nested/related properties:** support dotted field paths (e.g., `Assignee.LastName`) for joins.

### Migration when promoted
- Add the package, reference it from `IssueTracker.Application`.
- Replace the inline `IssueQueryTranslator` internals with delegating calls (keep the thin per-entity
  translator for the field-map + default sort).
- Simplify `GetIssues.Validator` to call `QueryableFilterBuilder.HasUnknownFilterField`.
- Remove the now-duplicated expression-building code from the sample.

### Source references
- Reference implementation: `samples/IssueTracker/IssueTracker.Application/Features/Issues/IssueQueryTranslator.cs`
- Consumer: `samples/IssueTracker/IssueTracker.Application/Features/Issues/GetIssues.cs`
- Core description types: `src/D20Tek.Vertically/Queries/Pagination/*`

---

## 2. Registration Origin Tracking + Diagnostics for `HandlerRegistration`

### Status
Proposed. No implementation yet. Motivated by the observation that once registrations collapse into
`ServiceDescriptor`s, there is no way to tell *how* a handler/validator got registered.

### Motivation
Today `RegisterFromAssembly` discovers registrations through two distinct paths (feature self-registration
in phase 1, loose assembly scan in phase 2), and there is also explicit per-type registration via
`IHandlerRegistrationBuilder`. All of these converge on the same `HandlerRegistration` record and are then
materialized into plain `ServiceDescriptor`s, which carry no provenance metadata. As a result, developers
cannot inspect the built `ServiceCollection` (or the Vertically builder) to answer "did this handler come
from an `IFeature`, a scan, or a manual registration?" This makes debugging registration surprises
(duplicates, unexpected discoveries, missing handlers) harder than it should be.

### Proposed change
Add an origin flag to `HandlerRegistration` (and the parallel validator registration data) capturing how
each entry was registered:

```csharp
public enum RegistrationSource
{
	Feature,    // registered via IFeature.Register (phase 1 discovery)
	Scan,       // discovered by the loose assembly scan (phase 2)
	Manual,     // registered explicitly via IHandlerRegistrationBuilder.Add<...>()
	Generated   // future: emitted by a source generator (see below)
}
```

- Set `Feature` when a feature's `Register` call adds the registration (phase 1).
- Set `Scan` for entries added by the phase 2 loose scan.
- Set `Manual` for the explicit `Add<THandler>()` / `AddValidator<TValidator>()` registration paths.
- Reserve `Generated` for a future source-generated registration path (avoids reflection/scanning; ties in
  with the AOT/trim considerations noted in feature #1).

### Developer-facing surface
Two complementary ways to consume the origin data:

1. **Inspect the builder data directly** — expose the collected registrations (with their
   `RegistrationSource`) as a read-only list on the builder so tests/tools can assert on provenance
   during composition.
2. **Diagnostic dump method** — a helper (e.g., `builder.DumpRegistrations()` or a
   `VerticallyDiagnostics.PrintRegistrations(...)`) that writes a formatted table to the console/logger
   for debug purposes: request type, handler impl, command/query, and the `RegistrationSource`. Intended
   for on-demand debugging, not always-on.

### Design notes / open questions
- `ServiceDescriptor` has no metadata slot, so origin must be captured at registration time on the
  Vertically-side records (before `Build()` materializes descriptors) and surfaced from the builder, not
  the `ServiceCollection`.
- Decide whether the diagnostics live in the core package or a separate `*.Diagnostics` companion to keep
  the core surface lean.
- Consider whether validators need the same origin tracking as handlers (likely yes, for symmetry).
- Consider a duplicate/overlap report (feature + scan registering the same type) as a natural extension.

### Source references
- Registration record: `src/D20Tek.Vertically/Registration/HandlerRegistration.cs`
- Two-phase discovery: `src/D20Tek.Vertically/Registration/HandlerRegistrationBuilder.cs`
- Builder materialization: `src/D20Tek.Vertically/Registration/VerticallyBuilder.cs` (`Build`)

---

## Idea Parking Lot

Unstructured list of candidate features to investigate later. Each will be expanded into a full entry
(Status / Motivation / Proposed surface / Design notes / Source references) when picked up.

### Pipeline / cross-cutting behaviors
- Caching behavior for queries (`ICacheableQuery` marker + key/TTL, backed by `IMemoryCache`/`IDistributedCache`).
- Retry / transient-fault behavior (configurable count, backoff, retriable `ErrorType`s/exceptions).
- Idempotency behavior for commands (idempotency key short-circuits duplicate submissions).
- Transaction / Unit-of-Work behavior (`ITransactionalCommand` marker; commit on success, roll back on failure).
- Authorization behavior (`IAuthorizer<TRequest>` checks short-circuit with Forbidden/Unauthorized result).
- Metrics / diagnostics behavior (`System.Diagnostics.Metrics` counters + `ActivitySource`/OpenTelemetry spans).
- Event Dispatch Behavior (deferred, in-process event dispatch): after a handler succeeds, drain the events
  raised during the slice and publish them via `INotificationPublisher`. In-memory staging only - if the
  process crashes after commit but before publish, events are lost (no durability guarantee). This is deferred
  in-process dispatch, NOT the Outbox pattern; it shares the event-collection front end and is the growth path
  toward a persisted transactional Outbox (see below). Formerly described as the post-processing/notification
  behavior.
- Post-processing behavior (`IPostProcessor<TRequest, TResult>` hooks after success, for concerns other than
  event dispatch such as cache invalidation or audit).

### Domain / result helpers
- Domain event dispatch (`IDomainEvent` + `IDomainEventHandler<T>`, collected and dispatched after the slice).
- Notification dispatcher (`INotificationPublisher` + `INotificationHandler<TNotification>` for 1:N in-process
  domain-event fan-out across aggregates/slices). Distinct from command/query direct injection, which stays 1:1;
  notifications are 1:N and fire-and-forget, so a thin publisher is the right tool without reintroducing a
  request/response dispatcher. Command handlers can inject `INotificationPublisher` and publish inline directly
  (best for simpler domains, no behavior needed); this also gives a growth path to raising events on aggregates
  and letting the Event Dispatch Behavior drain and publish them after a successful result - both paths use the
  same publisher so adoption is additive. Pairs with domain event dispatch (contracts) and the Event Dispatch
  Behavior (trigger). Design notes: in-process first with a transport-agnostic interface, sequential vs. parallel
  dispatch, error aggregation, and publish inside vs. after transaction.
- Transactional Outbox (persisted): stage domain events into an outbox table within the same DB transaction as
  the aggregate change, then a background relay reads and publishes them with retries and idempotency. Provides
  at-least-once delivery that survives a crash after commit - the durability guarantee the in-memory Event
  Dispatch Behavior does not offer. Shares the event-collection front end with that behavior, so the in-memory
  path is a stepping stone: swap the "drain and publish immediately" tail for "persist in the transaction +
  relay." Design notes: outbox schema/EF Core integration, relay hosting (BackgroundService), dedup/idempotency
  keys, ordering, and poison-message handling; likely a companion package alongside the EF Core translator.
- `D20Tek.Vertically.AspNetCore` companion package (promote sample `ResultHttpExtensions` / RFC 7807 mapping).
- Minimal API endpoint mapping helpers (`MapCommand<...>()` / `MapQuery<...>()` wiring to injected handlers).

### Query / pagination helpers
- In-memory `IEnumerable`/`IQueryable` translator (provider-neutral counterpart to the EF Core translator).
- Cursor encoding helpers (keyset cursor codec for `CursorPagedRequest`/`CursorPageOf<T>`).
- `PageOf<T>` async projection (`MapAsync(...)` companion to `Map`).

### Testing & tooling
- `D20Tek.Vertically.Testing` package (handler test harness through the real pipeline + result assertion helpers).
- Registration validation / analyzers (flag missing handlers, ambiguous result pairings, orphan validators).
- Source-generated registration (AOT/trim-safe alternative to reflection-based `RegisterFromAssembly`).

### Cross-cutting configuration
- Per-request behavior filtering by marker/attribute (behaviors opt in/out selectively).
- Streaming queries (`IStreamQuery<TResult>` + handler returning `IAsyncEnumerable<TResult>`).

