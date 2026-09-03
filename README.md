[![Build Status](https://github.com/d20Tek/Vertically/actions/workflows/ci-build.yml/badge.svg)](https://github.com/d20Tek/Vertically/actions)
[![NuGet](https://img.shields.io/nuget/v/D20Tek.Vertically)](https://www.nuget.org/packages/D20Tek.Vertically)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

# D20Tek.Vertically

A modern, lightweight .NET library for building applications with vertical slice architecture. Vertically lets you write a feature once as a self-contained slice (its command or query, validator, and handler) and then run that same slice from any host: a Web API, a Blazor app, or a command-line tool. It provides typed command and query handlers, a composable pipeline of cross-cutting behaviors, first-class validation, and dependency-injection registration, so your application logic stays focused, testable, and free of host-specific concerns.

Vertically is built around the idea that a feature is a vertical unit of behavior rather than a layer. Instead of spreading a single operation across controllers, services, and repositories, each slice groups everything that operation needs in one place. Handlers return a `Result<T>` rather than throwing for expected failures, which keeps success and failure paths explicit and lets every host translate outcomes into its own idiom (HTTP status codes, UI state, or process exit codes).

## Table of Contents

- [Features](#features)
- [Supported Platforms](#supported-platforms)
- [Installation](#installation)
- [Quick Start Guide](#quick-start-guide)
- [Usage](docs/getting-started-detailed.md#usage)
  - [Defining Commands and Queries](docs/getting-started-detailed.md#defining-commands-and-queries)
  - [Writing Handlers](docs/getting-started-detailed.md#writing-handlers)
  - [Dispatching a Slice](docs/getting-started-detailed.md#dispatching-a-slice)
  - [Adding Validation](docs/getting-started-detailed.md#adding-validation)
  - [Bundling a Slice as a Feature](docs/getting-started-detailed.md#bundling-a-slice-as-a-feature)
  - [Paged, Sorted, and Filtered Queries](docs/getting-started-detailed.md#paged-sorted-and-filtered-queries)
- [Configuration](docs/getting-started-detailed.md#configuration)
  - [Registering Handlers](docs/getting-started-detailed.md#registering-handlers)
  - [Selecting Behaviors](docs/getting-started-detailed.md#selecting-behaviors)
  - [Behavior Ordering and Placement](docs/getting-started-detailed.md#behavior-ordering-and-placement)
  - [Writing a Custom Behavior](docs/getting-started-detailed.md#writing-a-custom-behavior)
- [API Reference](docs/api-reference.md)
- [Sample Applications](#sample-applications)
- [License](#license)

## Why This Library Exists

Most .NET applications organize code by technical layer: controllers, services, repositories, and so on. As an application grows, a single feature ends up scattered across many of these layers, and changing one behavior means touching files in several folders. This layered approach also tends to encourage broad, shared service classes that accumulate unrelated responsibilities over time.

Vertical slice architecture takes the opposite view: organize by feature, not by layer. Each slice owns the request, the validation, and the handling logic for exactly one operation. This keeps related code together, makes features easy to find and reason about, and lets slices evolve independently.

Doing this well in .NET usually means writing repetitive plumbing:

- You need a consistent way to dispatch a request to its handler.
- You need cross-cutting concerns (logging, timing, validation, exception handling) applied uniformly without copying code into every handler.
- You need validation to run before the handler, with a predictable result shape.
- You need a clean way to register all of this with dependency injection.
- You want the same feature to run from different hosts without rewriting it per host.

Vertically provides these building blocks so you can focus on the slice itself.

This is not just another MediatR clone. There is no central dispatcher or `IMediator` abstraction sitting between your presentation code and your handlers. Instead, you inject the specific `ICommandHandler<TCommand, TResult>` or `IQueryHandler<TQuery, TResult>` directly into your presentation components and call it. Because the dependency is explicit and strongly typed, you get compile-time discovery of the exact handler being used, and stepping into a call in the debugger takes you straight to the handler rather than through a runtime dispatch and reflection layer. The cross-cutting pipeline is applied as decorators during dependency-injection registration, so behaviors wrap your handlers transparently without adding a dispatch call or an extra abstraction to your usage.

D20Tek.Vertically provides:

- A typed command and query handler model
- A composable pipeline of reusable cross-cutting behaviors
- Built-in validation that runs before your handler
- A self-registering feature model for grouping a slice's parts
- Host-agnostic results using `Result<T>` for explicit success and failure
- Dependency-injection registration with assembly scanning
- Ready-made pagination, sorting, and filtering primitives for queries

It gives .NET developers a first-class, standard way to build feature-focused applications that run unchanged across API, web, and CLI hosts.

## Features

- **Typed commands and queries**: Model each operation as an `ICommand<TResult>` or `IQuery<TResult>` handled by a matching `ICommandHandler` or `IQueryHandler`. The result type travels with the request so handlers, behaviors, and registration can discover the pairing automatically.
- **Result-based handling**: Handlers return `Result<T>` rather than throwing for expected failures, so success, validation, and error paths stay explicit and each host can translate them into its own response idiom.
- **Composable pipeline behaviors**: Wrap handlers with reusable cross-cutting behaviors through the `IPipelineBehavior<TRequest, TResult>` abstraction, with built-in behaviors for logging, timing, validation, and exception-to-result translation.
- **First-class validation**: Implement `IValidator<T>` and let the validation behavior run it before the handler, short-circuiting to a validation failure result when input is invalid.
- **Self-registering features**: Group a slice's command or query, handler, validator, and any slice-specific services into a single `IFeature` that registers itself, discovered during assembly scanning.
- **Host-owned behavior policy**: Handler registration lives in your application layer, while each host chooses which pipeline behaviors to apply, so the same slices can run with different cross-cutting policies per host.
- **Easy handler debugging**: Handlers are injected directly into presentation code as strongly-typed dependencies, so stepping into a call in the debugger lands straight on the handler with no central dispatcher or reflection layer to step through.
- **No-reflection dispatch**: Because you call the injected handler directly rather than routing through a mediator, there is no runtime type lookup or reflection to resolve the handler per request, which keeps the hot path allocation-free and predictable.
- **Flexible behavior placement**: Add behaviors as innermost or outermost, or relative to an existing behavior, to control pipeline ordering precisely.
- **Pagination, sorting, and filtering**: Use built-in paged request types, sort expressions, and a composable filter model to build consistent query endpoints.
- **Dependency-injection friendly**: Register everything through a single `AddVertically` entry point with assembly scanning and per-handler behavior configuration.
- **Lightweight and focused**: Minimal dependencies beyond D20Tek.Functional and the standard Microsoft.Extensions abstractions.

## Supported Platforms

| Target Framework | Status |
|---|---|
| .NET 9.0 | Supported |
| .NET 10.0 | Supported |

Vertically is host-agnostic and works with any .NET application model, including ASP.NET Core Minimal APIs, Blazor, worker services, and console applications.

## Installation

Install the package via the .NET CLI:

```bash
dotnet add package D20Tek.Vertically
```

Or via the NuGet Package Manager in Visual Studio:

```
Install-Package D20Tek.Vertically
```

## Quick Start Guide

### 1. Define a slice

Group the request, its validator, and its handler into a single self-registering feature. Handlers return a `Result<T>`, so expected failures are values rather than exceptions.

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

### 2. Register Vertically in Program.cs

Register handlers from your assembly and choose the pipeline behaviors this host should apply.

```csharp
using D20Tek.Vertically;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVertically(v =>
{
    v.Handlers.RegisterFromAssembly(typeof(CreateProduct).Assembly);
    v.Behaviors.AddLogging()
               .AddValidation()
               .AddExceptionToResult();
});

var app = builder.Build();
app.Run();
```

### 3. Dispatch a slice from a host

Resolve the handler and translate its `Result<T>` into the host's response idiom.

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

## Usage and Configuration

For detailed usage instructions covering commands, queries, validation, self-registering features, pipeline behaviors, and the pagination, sorting, and filtering primitives, as well as configuration options such as handler registration, behavior selection, and behavior ordering, see the [Detailed Getting Started Guide](docs/getting-started-detailed.md).

## API Reference

For a complete reference of all public interfaces, methods, behaviors, extension methods, and types, see the [API Reference](docs/api-reference.md).

## Sample Applications

The repository includes an end-to-end sample suite that demonstrates the core value of Vertically: writing a feature once and running it from multiple hosts.

### [Issue Tracker](samples/IssueTracker)

A lightweight issue tracker whose Application and Persistence layers (EF Core with SQLite) define a single set of vertical slices for creating, assigning, editing, and querying issues and users. Three separate hosts consume those same slices without duplicating any business logic:

- **IssueTracker.Api**: A Minimal API host that maps each slice to an endpoint and translates `Result<T>` into HTTP responses using RFC 7807 problem details, with an OpenAPI document and a Scalar reference UI.
- **IssueTracker.Web**: A Blazor Web App host (interactive server rendering) that dispatches the same slices from components and translates `Result<T>` into inline UI state.
- **IssueTracker.Cli**: A console host built with System.CommandLine that dispatches the same slices from verbs and translates `Result<T>` into standard output and process exit codes.

**Concepts demonstrated:** self-registering features, host-owned behavior policy, validation, `Result<T>` translation per host, and paged, sorted, and filtered queries.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
