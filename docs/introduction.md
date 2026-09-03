# Introducing D20Tek.Vertically

## The problem: features shouldn't be scattered across layers

If you've built anything beyond a small .NET application, you've felt the friction of layered architecture. A single feature - "create an order," "assign an issue," "list products with paging" - starts in a controller, flows into a service, drops into a repository, and picks up validation and cross-cutting concerns somewhere along the way. The logic for one operation ends up spread across several folders, and changing that operation means touching all of them. Over time, shared service classes swell with unrelated responsibilities, and it gets harder to see where a feature begins and ends.

Vertical slice architecture is the well-known answer: organize code by feature rather than by technical layer. Each slice owns exactly one operation, from request to result, so everything that changes together lives together. The idea is simple, but doing it well in .NET usually means writing the same plumbing over and over: a way to dispatch a request to its handler, a consistent place for validation, uniform cross-cutting behavior, and clean dependency-injection registration for all of it.

Many teams reach for a mediator library to fill that gap. That works, but it introduces a central dispatcher, runtime handler resolution, and an extra layer of indirection between your presentation code and the logic you actually care about. D20Tek.Vertically was built to give you the vertical-slice building blocks without that indirection.

## What this library does

D20Tek.Vertically provides a small, focused set of abstractions for building applications as vertical slices. You model each operation as an `ICommand<TResult>` or `IQuery<TResult>`, implement its logic in an `ICommandHandler` or `IQueryHandler`, and optionally add an `IValidator<T>`. You can group a slice's request, validator, and handler into a single self-registering `IFeature`, then register everything through one `AddVertically` call with assembly scanning.

Handlers return a `Result<TResult>` rather than throwing for expected failures, so success, validation, and error paths stay explicit as values. Each host - a Web API, a Blazor app, a console tool - resolves the specific handler it needs and translates that result into its own idiom: an HTTP status code, a piece of UI state, or a process exit code. The same slice runs unchanged across all of them.

Cross-cutting concerns are handled by a composable pipeline. Behaviors such as logging, timing, validation, and exception-to-result translation wrap your handlers as decorators, applied at registration time. You choose which behaviors apply, globally or per handler, and in what order.

## Where the mediator approach falls short

Mediator libraries popularized request/handler dispatch in .NET, and they served that role well. But they carry design decisions that add friction in practice.

The most notable is the central dispatcher. To invoke a handler, you inject an `IMediator` (or similar) and send a request object; the mediator then resolves the matching handler at runtime, usually through reflection and a dictionary of registrations. This puts an abstraction between your presentation code and your logic. When you step into a call in the debugger, you land in the mediator's internals rather than on your handler, and "find the handler for this request" becomes a runtime lookup rather than a compile-time fact.

D20Tek.Vertically takes a different approach: there is no central dispatcher. You inject the specific closed handler interface - for example `ICommandHandler<CreateOrder.Command, OrderResponse>` - directly into your component or endpoint and call `HandleAsync`. Because the dependency is explicit and strongly typed, the compiler tells you exactly which handler is in play, stepping into the call takes you straight to the handler, and there is no per-request reflection or type resolution on the hot path. The cross-cutting pipeline is still there, but it is applied as decorators during dependency-injection registration, so behaviors wrap your handler transparently without adding a dispatch call or an extra abstraction to your usage.

## Problems it solves

**Scattered feature logic.** By modeling each operation as a self-contained slice, the request, its validation, and its handler live in one place. Features are easy to find, easy to reason about, and free to evolve independently.

**Repetitive cross-cutting code.** Instead of copying logging, timing, or exception handling into every handler, you enable built-in behaviors once. `AddLogging()`, `AddTiming()`, `AddValidation()`, and `AddExceptionToResult()` wrap your handlers uniformly, and you can add your own behaviors by implementing a single `IPipelineBehavior<TRequest, TResult>` contract.

**Host-specific policy without host-specific logic.** Handler registration lives in your application layer, while each host chooses which behaviors to apply. The same slices can run with rich logging and timing in one host and a leaner pipeline in another, with no change to the slice itself.

**Validation that stays explicit.** Implement `IValidator<T>` or `IAsyncValidator<T>` and the validation behavior runs it before your handler, short-circuiting to a validation failure result when input is invalid. Validation runs only when validators are registered, so nothing happens by surprise.

**Consistent paged queries.** Building list endpoints usually means reinventing paging, sorting, and filtering. The library ships offset and cursor paged request types, `PageOf<T>` and `CursorPageOf<T>` results, sort expressions, and a composable, provider-agnostic filter tree - each with request validators - so your query slices stay consistent.

**Debuggability and predictable performance.** Because handlers are injected and called directly, debugging steps straight into your code, and there is no reflection-based dispatch to resolve a handler on each request.

## What it doesn't try to do

D20Tek.Vertically is a set of abstractions and registration helpers, not a framework that owns your application. It does not provide a data-access layer, an HTTP stack, or a UI. It deliberately has no central dispatcher and no runtime message bus, so it is not a drop-in replacement for the in-process or out-of-process messaging features that some mediator libraries offer beyond simple request/handler dispatch.

It also does not translate the provider-agnostic sort and filter model to a specific data provider for you. Those types describe intent; an adapter in your own persistence layer (for example an EF Core translator) resolves fields and applies operators against your model. This keeps the core library free of provider dependencies and lets you control exactly how queries are executed.

## Getting started

The library targets .NET 9.0 and .NET 10.0. Installation is a single package reference:

```bash
dotnet add package D20Tek.Vertically
```

Define a slice as a self-registering feature:

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

public sealed record ProductResponse(Guid Id, string Name, decimal Price);
```

Register Vertically in `Program.cs`, scanning your assembly and selecting the behaviors this host should apply:

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

Inject the specific handler and dispatch it, translating the result into the host's response:

```csharp
app.MapPost("/products", async (
	CreateProduct.Command command,
	ICommandHandler<CreateProduct.Command, ProductResponse> handler,
	CancellationToken cancellationToken) =>
{
	var result = await handler.HandleAsync(command, cancellationToken);
	return result.Match(
		value => Results.Created($"/products/{value.Id}", value),
		errors => Results.BadRequest(errors));
});
```

That's the entire setup. From here you can add more slices, enable additional behaviors, tune behavior ordering per handler, or build paged query slices with the pagination, sorting, and filtering primitives.

## Links

- **NuGet package:** [D20Tek.Vertically on NuGet.org](https://www.nuget.org/packages/D20Tek.Vertically)
- **API Reference:** [Complete API Reference](api-reference.md)
- **Sample suite:** [Issue Tracker sample](../samples/IssueTracker)
- **Source and samples:** [GitHub repository](https://github.com/d20Tek/Vertically)
