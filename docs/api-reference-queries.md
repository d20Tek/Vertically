# API Reference: Queries (Pagination, Sorting, and Filtering)

This document covers the query support types in the `D20Tek.Vertically.Queries.Pagination` namespace: paged request types, paged result types, sort expressions, the provider-agnostic filter tree, and the request validators. These types are provider-agnostic; adapters (for example an EF Core query translator) resolve fields and apply operators against their own model.

See the [API Reference hub](api-reference.md) for the other reference documents.

## Table of Contents

- [Request Types](#request-types)
- [Result Types](#result-types)
- [Sorting](#sorting)
- [Filtering](#filtering)
- [Request Validators](#request-validators)

## Request Types

| Type | Base | Description |
|---|---|---|
| `IPagedRequest` | - | Marker interface for all paging request strategies. Lets handlers, behaviors, and registration recognize a request as paged without coupling to a specific strategy. |
| `PagedRequest` | `IPagedRequest` | Offset-based paging request. |
| `CursorPagedRequest` | `IPagedRequest` | Cursor/keyset paging request using an opaque cursor. |
| `SortedFilteredPagedRequest` | `PagedRequest` | An offset paging request that additionally carries sort and filter instructions. |
| `SortedFilteredCursorPagedRequest` | `CursorPagedRequest` | A cursor paging request that additionally carries sort and filter instructions. |

### PagedRequest members

| Member | Type | Description |
|---|---|---|
| `PageNumber` | `int` | The one-based page number to retrieve. Defaults to `DefaultPageNumber` (1). |
| `PageSize` | `int` | The number of items per page. Defaults to `DefaultPageSize` (20). |
| `Skip` | `int` | The number of items to skip to reach the requested page (`(PageNumber - 1) * PageSize`). |
| `Take` | `int` | The number of items to take (equal to `PageSize`). |
| `DefaultPageNumber` | `const int` | The default page number (1). |
| `DefaultPageSize` | `const int` | The default page size (20). |
| `MaxPageSize` | `const int` | The maximum page size a caller may request (100). |

### CursorPagedRequest members

| Member | Type | Description |
|---|---|---|
| `Cursor` | `string?` | The opaque cursor identifying the position to page from, or `null` for the first page. |
| `PageSize` | `int` | The number of items per page. Defaults to `DefaultPageSize` (20). |
| `DefaultPageSize` | `const int` | The default page size (20). |
| `MaxPageSize` | `const int` | The maximum page size a caller may request (100). |

### SortedFiltered request members

Both `SortedFilteredPagedRequest` and `SortedFilteredCursorPagedRequest` add the following members to their base request type.

| Member | Type | Description |
|---|---|---|
| `Sorts` | `IReadOnlyList<SortExpression>` | The ordered set of sort instructions to apply. |
| `Filter` | `FilterGroup?` | The root of the filter tree to apply, or `null` when no filtering is requested. |

## Result Types

Query handlers return `Result<PageOf<T>>` for offset paging and `Result<CursorPageOf<T>>` for cursor paging.

### PageOf\<T\>

A single page of items with the metadata needed to navigate an offset-paged data set.

| Member | Type | Description |
|---|---|---|
| `Items` | `IReadOnlyList<T>` | The items on this page. |
| `PageNumber` | `int` | The one-based number of this page. |
| `PageSize` | `int` | The number of items per page. |
| `TotalCount` | `long` | The total number of items across all pages. |
| `TotalPages` | `int` | The total number of pages given `TotalCount` and `PageSize`. |
| `HasPrevious` | `bool` | Whether a previous page exists. |
| `HasNext` | `bool` | Whether a next page exists. |
| `Create(IReadOnlyList<T> items, PagedRequest request, long totalCount)` | `PageOf<T>` | Creates a page deriving its metadata from the originating request. |
| `Empty(PagedRequest request)` | `PageOf<T>` | Creates an empty page that preserves the paging metadata of the request. |
| `Map<TOut>(Func<T, TOut> selector)` | `PageOf<TOut>` | Projects each item to a new type while preserving this page's navigation metadata. |

### CursorPageOf\<T\>

A single page of items produced by cursor/keyset paging, with the opaque cursors needed to navigate.

| Member | Type | Description |
|---|---|---|
| `Items` | `IReadOnlyList<T>` | The items on this page. |
| `PageSize` | `int` | The number of items per page that was requested. |
| `NextCursor` | `string?` | The opaque cursor for the next page, or `null` when there is no next page. |
| `PreviousCursor` | `string?` | The opaque cursor for the previous page, or `null` when there is no previous page. |
| `HasNext` | `bool` | Whether a next page exists, derived from `NextCursor`. |
| `HasPrevious` | `bool` | Whether a previous page exists, derived from `PreviousCursor`. |
| `Create(IReadOnlyList<T> items, CursorPagedRequest request, string? nextCursor = null, string? previousCursor = null)` | `CursorPageOf<T>` | Creates a page deriving the page size from the originating request. |
| `Empty(CursorPagedRequest request)` | `CursorPageOf<T>` | Creates an empty page that preserves the page size of the request. |
| `Map<TOut>(Func<T, TOut> selector)` | `CursorPageOf<TOut>` | Projects each item to a new type while preserving this page's cursors and metadata. |

## Sorting

| Type | Description |
|---|---|
| `SortExpression(string Field, SortDirection Direction = SortDirection.Ascending)` | A provider-agnostic sort instruction. Adapters resolve `Field` against their own model and order by it in the given `Direction`. |
| `SortDirection` | Enum: `Ascending` (0), `Descending` (1). |

## Filtering

The filter tree is a closed hierarchy: a node is either a single `FilterExpression` leaf or a `FilterGroup` that combines child nodes, enabling arbitrarily nested AND/OR logic.

| Type | Base | Description |
|---|---|---|
| `FilterNode` | - | Abstract base type for a node in the filter tree. The hierarchy is closed, so consumers can exhaustively translate every node kind. |
| `FilterExpression(string Field, FilterOperator Operator, object? Value)` | `FilterNode` | A leaf filter instruction. Adapters resolve `Field`, coerce `Value`, and apply the `Operator`. |
| `FilterGroup(FilterLogic Logic, IReadOnlyList<FilterNode> Nodes)` | `FilterNode` | A composite node combining its `Nodes` with a boolean `Logic` operator. Groups can nest to express arbitrary AND/OR trees. |

### FilterGroup factory methods

| Method | Return Type | Description |
|---|---|---|
| `FilterGroup.All(params FilterNode[] nodes)` | `FilterGroup` | Creates a group that requires all supplied nodes to match (AND). |
| `FilterGroup.Any(params FilterNode[] nodes)` | `FilterGroup` | Creates a group that requires at least one supplied node to match (OR). |

### FilterLogic

Enum used by a `FilterGroup` to combine its child nodes.

| Value | Description |
|---|---|
| `And` (0) | All child nodes must match. |
| `Or` (1) | At least one child node must match. |

### FilterOperator

The comparison a `FilterExpression` applies to a field.

| Value | Description |
|---|---|
| `Equals` (0) | Field equals the value. |
| `NotEquals` (1) | Field does not equal the value. |
| `GreaterThan` (2) | Field is greater than the value. |
| `GreaterThanOrEqual` (3) | Field is greater than or equal to the value. |
| `LessThan` (4) | Field is less than the value. |
| `LessThanOrEqual` (5) | Field is less than or equal to the value. |
| `Contains` (6) | Field contains the value. |
| `StartsWith` (7) | Field starts with the value. |
| `EndsWith` (8) | Field ends with the value. |
| `In` (9) | Field is contained in the collection value. |
| `NotIn` (10) | Field is not contained in the collection value. |
| `Between` (11) | Field is between the two supplied values. |
| `IsNull` (12) | Field is null. |
| `IsNotNull` (13) | Field is not null. |

## Request Validators

Each of these is a `sealed class` implementing `IValidator<T>` for its corresponding request type, enforcing page-size bounds and other request constraints. Register them via the handler registration builder as needed.

| Validator | Validates |
|---|---|
| `PagedRequestValidator` | `PagedRequest` |
| `CursorPagedRequestValidator` | `CursorPagedRequest` |
| `SortedFilteredPagedRequestValidator` | `SortedFilteredPagedRequest` |
| `SortedFilteredCursorPagedRequestValidator` | `SortedFilteredCursorPagedRequest` |
