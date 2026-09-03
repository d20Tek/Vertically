# Detailed Getting Started Guide

This document provides detailed usage instructions and configuration options for D20Tek.Vertically. For a quick-start guide, see the [Quick Start](../README.md#quick-start-guide) section in the main README. For a conceptual overview, see the [Introduction](introduction.md), and for the full type-by-type listing, see the [API Reference](api-reference.md).

The examples below follow the main line of everyday usage. They do not cover every type in the package; the [API Reference](api-reference.md) documents the complete surface.

## Table of Contents

- [Usage](#usage)
  - [Defining Commands and Queries](#defining-commands-and-queries)
  - [Writing Handlers](#writing-handlers)
  - [Dispatching a Slice](#dispatching-a-slice)
  - [Adding Validation](#adding-validation)
  - [Bundling a Slice as a Feature](#bundling-a-slice-as-a-feature)
  - [Paged, Sorted, and Filtered Queries](#paged-sorted-and-filtered-queries)
- [Configuration](#configuration)
  - [Registering Handlers](#registering-handlers)
  - [Selecting Behaviors](#selecting-behaviors)
  - [Behavior Ordering and Placement](#behavior-ordering-and-placement)
  - [Writing a Custom Behavior](#writing-a-custom-behavior)

## Usage

### Defining Commands and Queries

A slice starts with a request. Model an operation that changes state as an `ICommand<TResult>` and a read operation as an `IQuery<TResult>`. The result type travels with the request, so handlers, behaviors, and registration can discover the request-to-result pairing from the request alone. Records are a natural fit because requests are immutable data.

```csharp
using D20Tek.Vertically;

// A command that changes state and returns the created resource.
public sealed record CreateProduct(string Name, decimal Price) : ICommand<ProductResponse>;

// A query that reads and returns a resource.
public sealed record GetProductById(Guid Id) : IQuery<ProductResponse>;

public sealed record ProductResponse(Guid Id, string Name, decimal Price);
```

Both `TResult` type arguments are constrained to `notnull`, so a request always produces a concrete result type.

### Writing Handlers

A handler implements the logic for exactly one request. Command handlers implement `ICommandHandler<TCommand, TResult>` and query handlers implement `IQueryHandler<TQuery, TResult>`. Both expose a single `HandleAsync` method that returns a `Result<TResult>`.

Returning a `Result<TResult>` (from D20Tek.Functional) is central to how Vertically works: expected failures - not found, conflict, validation - are returned as values rather than thrown as exceptions. This keeps success and failure paths explicit and lets each host translate the outcome into its own idiom.

```csharp
using D20Tek.Vertically;

public sealed class CreateProductHandler : ICommandHandler<CreateProduct, ProductResponse>
{
	private readonly IProductRepository _repository;

	public CreateProductHandler(IProductRepository repository) => _repository = repository;

	public async Task<Result<ProductResponse>> HandleAsync(
		CreateProduct command,
		CancellationToken cancellationToken = default)
	{
		var product = await _repository.AddAsync(command.Name, command.Price, cancellationToken);
		return new ProductResponse(product.Id, product.Name, product.Price);
	}
}
```

A `Result<TResult>` implicitly converts from the success value, so returning the response directly (as above) produces a success result. To return a failure, return a `Result<TResult>.Failure(...)` with an `Error`:

```csharp
public async Task<Result<ProductResponse>> HandleAsync(
	GetProductById query,
	CancellationToken cancellationToken = default)
{
	var product = await _repository.FindAsync(query.Id, cancellationToken);
	if (product is null)
	{
		return Result<ProductResponse>.Failure(
			Error.NotFound("Product.NotFound", $"Product '{query.Id}' was not found."));
	}

	return new ProductResponse(product.Id, product.Name, product.Price);
}
```

An `Error` carries a `Code`, a `Message`, and a `Type` (an `ErrorType` such as `NotFound`, `Conflict`, `Validation`, or `Unexpected`). Hosts use the `Type` to decide how to translate a failure.

### Dispatching a Slice

There is no central dispatcher. You inject the specific closed handler interface directly into your presentation code - an endpoint, a Blazor component, or a CLI verb - and call `HandleAsync`. Because the dependency is explicit and strongly typed, the compiler tells you exactly which handler runs, and stepping into the call in the debugger lands on your handler rather than on framework internals.

The idiomatic way to resolve a `Result<T>` is `Match`, which takes a success delegate and a failure delegate (the failure delegate receives the `Error[]`). This keeps both paths explicit and avoids unsafe value access.

In a Minimal API endpoint:

```csharp
app.MapPost("/products", async (
	CreateProduct command,
	ICommandHandler<CreateProduct, ProductResponse> handler,
	CancellationToken cancellationToken) =>
{
	var result = await handler.HandleAsync(command, cancellationToken);
	return result.Match(
		value => Results.Created($"/products/{value.Id}", value),
		errors => Results.BadRequest(errors));
});
```

In a Blazor component, inject the handler and translate the result into UI state:

```razor
@inject IQueryHandler<GetProductById, ProductResponse> GetProduct

@code {
	private ProductResponse? _product;
	private string? _error;

	protected override async Task OnInitializedAsync()
	{
		var result = await GetProduct.HandleAsync(new GetProductById(Id));
		if (result.IsSuccess)
		{
			_product = result.GetValue();
		}
		else
		{
			_error = result.GetErrors()[0].Message;
		}
	}
}
```

Each host maps a success to its own response and each `Error` classification (the `ErrorType`) to an appropriate outcome - an HTTP status code, a piece of UI state, or a process exit code. The slice itself never changes across hosts. When you only need the success state, `IsSuccess` and `IsFailure` are also available, and `GetValue()`, `DefaultValue(fallback)`, and `GetErrors()` provide direct access when you have already checked the state.

### Adding Validation

Input validation lives in a validator, kept separate from the handler. Implement `IValidator<T>` for synchronous checks or `IAsyncValidator<T>` when validation needs I/O or a remote call. A validator returns a `ValidationErrors` collection.

```csharp
using D20Tek.Vertically;

public sealed class CreateProductValidator : IValidator<CreateProduct>
{
	public ValidationErrors Validate(CreateProduct input)
	{
		var errors = ValidationErrors.Create();
		errors.AddIfError(() => string.IsNullOrWhiteSpace(input.Name), nameof(input.Name), "Name is required.");
		errors.AddIfError(() => input.Price <= 0, nameof(input.Price), "Price must be greater than zero.");
		return errors;
	}
}
```

When the validation behavior is enabled (see [Selecting Behaviors](#selecting-behaviors)), it runs any registered validators for a request before the handler and short-circuits to a validation failure result if there are errors. Validation stays explicit: the behavior only runs when a validator is registered for the request, so nothing happens implicitly. Handlers may still validate inline when it is convenient.

### Bundling a Slice as a Feature

The request, handler, and validator for one operation can live in a single self-registering class that implements `IFeature`. Nesting the related types inside the feature keeps the whole slice in one file and gives it a single registration point. This is the recommended way to organize slices.

```csharp
using D20Tek.Vertically;
using D20Tek.Vertically.Registration;

public sealed class CreateProduct : IFeature
{
	public void Register(IVerticallyBuilder builder) =>
		builder.Handlers.AddCommandHandler<Handler>()
						.AddValidator<Validator>();

	public sealed record Command(string Name, decimal Price) : ICommand<ProductResponse>;

	public sealed class Validator : IValidator<Command>
	{
		public ValidationErrors Validate(Command input)
		{
			var errors = ValidationErrors.Create();
			errors.AddIfError(() => string.IsNullOrWhiteSpace(input.Name), nameof(Command.Name), "Name is required.");
			errors.AddIfError(() => input.Price <= 0, nameof(Command.Price), "Price must be greater than zero.");
			return errors;
		}
	}

	public sealed class Handler : ICommandHandler<Command, ProductResponse>
	{
		public Task<Result<ProductResponse>> HandleAsync(Command command, CancellationToken cancellationToken = default)
		{
			var response = new ProductResponse(Guid.NewGuid(), command.Name, command.Price);
			return Task.FromResult(Result<ProductResponse>.Success(response));
		}
	}
}
```

Because a static class cannot implement an interface, a feature is a non-static class, though its nested `Command`, `Handler`, and `Validator` types can be anything you like. Features are discovered during assembly scanning and register themselves through the `Register` method. For AOT and trim safety, use the explicit generic registration methods shown here and avoid nested assembly scans inside `Register`.

### Paged, Sorted, and Filtered Queries

List queries commonly need paging, and often sorting and filtering. The `D20Tek.Vertically.Queries.Pagination` namespace provides provider-agnostic request and result types so your query slices stay consistent.

Use `PagedRequest` for offset paging or `CursorPagedRequest` for cursor/keyset paging. A query returns a `PageOf<T>` or `CursorPageOf<T>`, which carries the items plus navigation metadata.

```csharp
using D20Tek.Vertically;
using D20Tek.Vertically.Queries.Pagination;

public sealed record ListProducts : PagedRequest, IQuery<PageOf<ProductResponse>>;

public sealed class ListProductsHandler : IQueryHandler<ListProducts, PageOf<ProductResponse>>
{
	private readonly IProductRepository _repository;

	public ListProductsHandler(IProductRepository repository) => _repository = repository;

	public async Task<Result<PageOf<ProductResponse>>> HandleAsync(
		ListProducts query,
		CancellationToken cancellationToken = default)
	{
		var (items, total) = await _repository.GetPageAsync(query.Skip, query.Take, cancellationToken);
		var responses = items.Select(p => new ProductResponse(p.Id, p.Name, p.Price)).ToList();

		return PageOf<ProductResponse>.Create(responses, query, total);
	}
}
```

`PagedRequest` exposes `Skip` and `Take` derived from `PageNumber` and `PageSize`, along with sensible defaults (`DefaultPageSize` of 20) and a `MaxPageSize` of 100. `PageOf<T>` computes `TotalPages`, `HasNext`, and `HasPrevious` for you, and its `Map` method projects items to another type while preserving the page metadata.

When you also need sorting and filtering, derive from `SortedFilteredPagedRequest` (or `SortedFilteredCursorPagedRequest`). These carry a list of `SortExpression` values and an optional `FilterGroup` describing a provider-agnostic filter tree:

```csharp
var request = new SortedFilteredPagedRequest
{
	PageNumber = 1,
	PageSize = 20,
	Sorts = [new SortExpression("Name", SortDirection.Ascending)],
	Filter = FilterGroup.All(
		new FilterExpression("Price", FilterOperator.GreaterThanOrEqual, 10m),
		new FilterExpression("Name", FilterOperator.Contains, "widget")),
};
```

The sort and filter types describe intent only. An adapter in your persistence layer (for example an EF Core translator) resolves each field against your model and applies the operators. The package also includes request validators such as `PagedRequestValidator` and `SortedFilteredPagedRequestValidator` that enforce page-size bounds; register them like any other validator when you want the validation behavior to guard these requests.

## Configuration

All configuration happens through a single `AddVertically` call in your host's startup. The callback receives an `IVerticallyBuilder` exposing two sub-builders: `Handlers` for registration and `Behaviors` for the global pipeline.

```csharp
using D20Tek.Vertically;

builder.Services.AddVertically(v =>
{
	v.Handlers.RegisterFromAssembly(typeof(CreateProduct).Assembly);
	v.Behaviors.AddLogging()
			   .AddValidation()
			   .AddExceptionToResult();
});
```

### Registering Handlers

The `Handlers` builder registers handlers and validators as Scoped services in three ways:

- **By assembly scanning.** `RegisterFromAssembly(assembly)` (or `RegisterFromAssemblies(...)`) scans an assembly, running any `IFeature` implementers first and then registering the remaining loose handlers and validators it finds. This is the usual choice for an application layer full of feature slices.
- **Explicitly by type.** `AddCommandHandler<THandler>()`, `AddQueryHandler<THandler>()`, and `AddValidator<TValidator>()` register a single type. Features use these methods inside their `Register` body.

```csharp
v.Handlers
	.RegisterFromAssembly(typeof(CreateProduct).Assembly)
	.AddValidator<PagedRequestValidator>();
```

### Selecting Behaviors

Behaviors are cross-cutting decorators that wrap your handlers. Handler registration lives in your application layer, but each host chooses which behaviors to apply, so the same slices can run under different cross-cutting policies. The built-in behaviors each have a convenience method on the `Behaviors` builder:

| Method | Behavior |
|---|---|
| `AddLogging()` | Logs the start and outcome (success or failure) of each request. |
| `AddTiming()` | Measures and logs the elapsed time of the handler and its inner behaviors. |
| `AddValidation()` | Runs registered validators before the handler and short-circuits on validation errors. |
| `AddExceptionToResult()` | Catches unexpected exceptions and maps them to a failure result so the Result contract holds end-to-end. |

```csharp
v.Behaviors
	.AddLogging()
	.AddTiming()
	.AddValidation()
	.AddExceptionToResult();
```

Behaviors added here are global: they apply to every handler.

### Behavior Ordering and Placement

Global behaviors are composed around the handler in registration order, outermost first. In the example above, a request flows in through logging, then timing, then validation, then exception-to-result, reaches the handler, and unwinds back out in reverse. Order matters: placing `AddExceptionToResult()` toward the inside, for instance, keeps its exception mapping close to the handler while still allowing outer behaviors to observe the resulting `Result`.

For finer control you can configure behaviors on a single handler using `ForCommand<TCommand>()` or `ForQuery<TQuery>()`. Behaviors added in a per-handler scope sit closest to the handler (innermost) by default, inside any global behaviors. Placement modifiers adjust where the next behavior lands:

```csharp
v.ForCommand<CreateProduct.Command>()
	.AddTiming()                          // innermost, closest to the handler
	.AtOutermost().AddLogging()           // outside all existing behaviors for this handler
	.InsertBefore(typeof(TimingBehavior<,>)).Add(typeof(AuditBehavior<,>));
```

- `AtOutermost()` places the next behavior outside all existing behaviors for that handler (it runs first on the way in).
- `InsertBefore(anchor)` places the next behavior immediately outside the given anchor behavior in that handler's pipeline.

### Writing a Custom Behavior

Both built-in and custom behaviors implement the same contract: `IPipelineBehavior<TRequest, TResult>`. Implement it as an open generic class so it can wrap any request. Call `next()` to continue toward the handler, or return a `Result<TResult>` failure without calling `next` to short-circuit the pipeline.

```csharp
using D20Tek.Vertically.Pipeline;

public sealed class AuditBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult>
	where TRequest : notnull
	where TResult : notnull
{
	private readonly IAuditLog _audit;

	public AuditBehavior(IAuditLog audit) => _audit = audit;

	public async Task<Result<TResult>> HandleAsync(
		TRequest request,
		RequestHandlerDelegate<TResult> next,
		CancellationToken cancellationToken = default)
	{
		await _audit.RecordAsync(typeof(TRequest).Name, cancellationToken);
		return await next();
	}
}
```

Register it globally with `v.Behaviors.Add(typeof(AuditBehavior<,>))` or per handler with `Add(typeof(AuditBehavior<,>))` inside a `ForCommand`/`ForQuery` scope. Behaviors are resolved as singletons by default. If your behavior resolves scoped services from the request scope, implement the `IScopedBehavior` marker so it is registered with a scoped lifetime instead, avoiding a captive-dependency bug (this is exactly how the built-in validation behavior resolves scoped validators).
