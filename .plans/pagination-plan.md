# D20Tek.Vertically — Pagination Design & Plan

> Status: Approved design, pending implementation
> Scope: A standard, provider-agnostic paging query contract for `D20Tek.Vertically`.
> Constraint: Core library stays UI- and persistence-agnostic (no EF Core / IQueryable
> execution logic in the core package).
> Target: **`net9.0` + `net10.0`** (multi-target, matching sibling packages),
> Central Package Management, MSTest.

---

## 1. Motivation

Paging is a near-universal query concern, yet every consuming app tends to reinvent the same
request/response shapes (page number, page size, total count, sort, filter). Providing a small,
standard vocabulary in `D20Tek.Vertically`:

- Removes boilerplate for consumers.
- Pairs naturally with the existing `Result<T>` return contract used by query handlers.
- Stays consistent with the package's abstraction-first, low-dependency philosophy — the core
  ships **contracts + value types only**, never persistence logic.

---

## 2. Key Design Decisions (with rationale)

### 2.1 Contracts in core, execution in an add-on
- **Decision:** The core `D20Tek.Vertically` package ships only the paging **contracts and
  value types** (request shapes, sort/filter expressions, and the `PageOf<T>` result). Any
  `IQueryable`/EF Core execution helpers (e.g., `ToPageOfAsync(request, ct)`) live in a
  **separate companion package** (e.g., `D20Tek.Vertically.EntityFrameworkCore`).
- **Why:** Keeps the core dependency-free and AOT/trim-friendly, consistent with the rest of
  the library. Persistence concerns opt in via a dedicated package.

### 2.2 Result naming: `PageOf<T>` (not `PagedResult<T>`)
- **Decision:** The paged result type is named **`PageOf<T>`**.
- **Why:** Query handlers return `Result<T>`, so `Result<PagedResult<T>>` stutters
  ("result result"). `Result<PageOf<T>>` reads cleanly as "a result of a page of T." The
  inner type only needs to describe the data shape (one page of items), not success/failure —
  that is already `Result<T>`'s job.

### 2.3 Offset-based first; open to cursor-based later
- **Decision:** Ship offset-based paging (`PageNumber` + `PageSize`) in the first cut. Keep the
  design open so cursor/keyset paging can be added later as a **separate request class**, not a
  breaking change to the existing shape.
- **Why:** Offset paging is the common, simplest standard and fits the majority of apps. Cursor
  paging scales better for large/infinite lists but has a different request shape; it should be
  additive.
- **Enabler:** A marker interface `IPagedRequest` lets handlers, behaviors, and registration
  recognize "this request is paged" without coupling to a specific paging strategy. A future
  `CursorPagedRequest` implements the same marker and participates without changes to existing
  contracts.

### 2.4 Provider-agnostic sorting & filtering
- **Decision:** Sorting and filtering are expressed as string-keyed, untyped value objects
  (`SortExpression`, `FilterExpression`) so the contract is decoupled from any concrete entity
  type. Adapters resolve field names and coerce values against their own model.
- **Why:** Keeps the core free of expression-tree / entity coupling and works across LINQ, SQL,
  and other backends. Concrete queries can still layer strongly-typed sort/filter on top if
  desired.
- **Layering:** Sorting/filtering live on a **subclass** of the base paging request, so callers
  who only need paging are not forced to reason about sort/filter.

### 2.5 Validation via existing `IValidator<T>`
- **Decision:** Paging requests are validated with the existing `IValidator<T>` contract and the
  standard `ValidationBehavior` — not with bespoke validation baked into the DTO.
- **Why:** Consistent with the library's validation story; guardrails (page bounds, max page
  size, non-empty sort/filter fields) flow through the same pipeline. Because `IValidator<in T>`
  is contravariant, a validator declared for the base request also validates subclasses.

---

## 3. Proposed Types

All types live under a new `Queries/` folder in the core package.

### 3.1 Marker
```csharp
// Queries/IPagedRequest.cs
public interface IPagedRequest { }   // marker for all paging strategies
```

### 3.2 Offset request (base)
```csharp
// Queries/PagedRequest.cs
public record PagedRequest : IPagedRequest
{
	public const int DefaultPageNumber = 1;
	public const int DefaultPageSize = 20;
	public const int MaxPageSize = 100;

	public int PageNumber { get; init; } = DefaultPageNumber;
	public int PageSize   { get; init; } = DefaultPageSize;

	public int Skip => (PageNumber - 1) * PageSize;
	public int Take => PageSize;
}
```

### 3.3 Sorting & filtering value types
```csharp
// Queries/SortDirection.cs
public enum SortDirection { Ascending = 0, Descending = 1 }

// Queries/SortExpression.cs
public sealed record SortExpression(string Field, SortDirection Direction = SortDirection.Ascending);

// Queries/FilterOperator.cs
public enum FilterOperator
{
	Equals = 0, NotEquals = 1,
	GreaterThan = 2, GreaterThanOrEqual = 3,
	LessThan = 4, LessThanOrEqual = 5,
	Contains = 6, StartsWith = 7, EndsWith = 8,
}

// Queries/FilterExpression.cs
public sealed record FilterExpression(string Field, FilterOperator Operator, object? Value);
```

### 3.4 Sorted/filtered request (subclass)
```csharp
// Queries/SortedFilteredPagedRequest.cs
public record SortedFilteredPagedRequest : PagedRequest
{
	public IReadOnlyList<SortExpression>   Sorts   { get; init; } = [];
	public IReadOnlyList<FilterExpression> Filters { get; init; } = [];
}
```

### 3.5 Paged result: `PageOf<T>`
```csharp
// Queries/PageOf.cs
public sealed record PageOf<T>
{
	public IReadOnlyList<T> Items { get; init; } = [];
	public int  PageNumber { get; init; }
	public int  PageSize   { get; init; }
	public long TotalCount { get; init; }

	public int  TotalPages  => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
	public bool HasPrevious => PageNumber > 1;
	public bool HasNext     => PageNumber < TotalPages;

	// Convenience factory that derives page metadata from the originating request.
	public static PageOf<T> Create(IReadOnlyList<T> items, PagedRequest request, long totalCount) =>
		new()
		{
			Items = items,
			PageNumber = request.PageNumber,
			PageSize = request.PageSize,
			TotalCount = totalCount,
		};
}
```

Handlers therefore return `Task<Result<PageOf<T>>>`.

### 3.6 Validators
```csharp
// Queries/PagedRequestValidator.cs
public sealed class PagedRequestValidator : IValidator<PagedRequest>
{
	public ValidationErrors Validate(PagedRequest input) =>
		ValidationErrors.Create()
			.AddIfError(() => input.PageNumber < 1,
				Error.Validation("PageNumber", "PageNumber must be greater than or equal to 1."))
			.AddIfError(() => input.PageSize < 1,
				Error.Validation("PageSize", "PageSize must be greater than or equal to 1."))
			.AddIfError(() => input.PageSize > PagedRequest.MaxPageSize,
				Error.Validation("PageSize", $"PageSize must not exceed {PagedRequest.MaxPageSize}."));
}
```
- A dedicated `SortedFilteredPagedRequestValidator : IValidator<SortedFilteredPagedRequest>`
  reuses the base bound checks and additionally rejects sort/filter expressions with empty
  `Field` values. (Field *allow-lists* are app-specific and stay out of the core validator.)
- Both map failures onto the existing `Error.Validation(code, message)` factory, consistent
  with the library's error conventions.

---

## 4. Proposed Repo / Package Shape

```
src/
  D20Tek.Vertically/
	Querying/
	  IPagedRequest.cs
	  PagedRequest.cs
	  SortedFilteredPagedRequest.cs
	  SortDirection.cs
	  SortExpression.cs
	  FilterOperator.cs
	  FilterExpression.cs
	  PageOf.cs
	  PagedRequestValidator.cs
	  SortedFilteredPagedRequestValidator.cs

  (future) D20Tek.Vertically.EntityFrameworkCore/
	  QueryableExtensions.cs        # ToPageOfAsync(request, ct): count + Skip/Take + sort/filter

tests/
  D20Tek.Vertically.Tests/
	Querying/                        # MSTest, mirrors source
```

---

## 5. Out of Scope (this pass)

- **Cursor/keyset paging** (`CursorPagedRequest`) — additive later via the `IPagedRequest`
  marker, separate request class.
- **`IQueryable`/EF Core execution helpers** — belong in the companion
  `D20Tek.Vertically.EntityFrameworkCore` package, not core.
- **Strongly-typed sort/filter builders** and **field allow-lists** — app-specific concerns.
- **Expression-tree translation** of `FilterExpression` — an adapter concern.

---

## 6. Implementation Plan (Steps)

1. [x] Create the `Querying/` folder in `src/D20Tek.Vertically`.
2. [x] Add `IPagedRequest` marker interface.
3. [x] Add `PagedRequest` (offset paging with `Skip`/`Take` and page-size constants).
4. [x] Add sort/filter value types: `SortDirection`, `SortExpression`, `FilterOperator`,
   `FilterExpression`.
5. [x] Add `SortedFilteredPagedRequest` subclass.
6. [x] Add `PageOf<T>` result record (computed metadata + `Create` factory).
7. [x] Add `PagedRequestValidator` and `SortedFilteredPagedRequestValidator` (using
   `Error.Validation`).
8. [x] Add MSTest coverage under `tests/D20Tek.Vertically.Tests/Querying/`
   (page math, request defaults/bounds, validator behavior, factory metadata).
9. [x] Build + run tests to validate.
10. (Future) Spin up `D20Tek.Vertically.EntityFrameworkCore` with `ToPageOfAsync` execution
	helpers.
11. [x] Add `CursorPagedRequest` implementing `IPagedRequest`.
12. [x] Add FilterGroup to support AND/OR grouping of filter expressions. This will allow for more complex filtering scenarios.
