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
