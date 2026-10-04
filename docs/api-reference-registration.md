# API Reference: Registration

This document covers the dependency-injection entry point and the fluent registration builders. The entry point lives in the `Microsoft.Extensions.DependencyInjection` namespace so it is discoverable on `IServiceCollection`; the builders live in `D20Tek.Vertically.Registration`.

See the [API Reference hub](api-reference.md) for the other reference documents.

## Table of Contents

- [DI Registration Methods](#di-registration-methods)
- [IVerticallyBuilder](#iverticallybuilder)
- [IHandlerRegistrationBuilder](#ihandlerregistrationbuilder)
- [IBehaviorRegistrationBuilder](#ibehaviorregistrationbuilder)
- [IHandlerBehaviorScope](#ihandlerbehaviorscope)
- [RegistrationSource](#registrationsource)
- [HandlerRegistrationInfo](#handlerregistrationinfo)
- [ValidatorRegistrationInfo](#validatorregistrationinfo)
- [VerticallyDiagnostics](#verticallydiagnostics)

## DI Registration Methods

The following extension method is defined in the `ServiceCollectionExtensions` static class and is available on any `IServiceCollection` instance.

| Method | Return Type | Description |
|---|---|---|
| `AddVertically(Action<IVerticallyBuilder> configure)` | `IServiceCollection` | Registers D20Tek.Vertically handlers, validators, and pipeline behaviors. Configure registration through the supplied callback (for example `builder.Handlers.RegisterFromAssembly(...)` and `builder.Behaviors.AddLogging()`). Throws `ArgumentNullException` when `services` or `configure` is `null`. |

## IVerticallyBuilder

The root fluent builder passed to the `AddVertically` callback. It exposes grouped sub-builders plus direct access to the underlying service collection for slice-specific registrations.

| Member | Type | Description |
|---|---|---|
| `Services` | `IServiceCollection` | The underlying service collection, for slice-specific service registrations. |
| `Handlers` | `IHandlerRegistrationBuilder` | Handler and validator registration (explicit, scanning, and feature discovery). |
| `Behaviors` | `IBehaviorRegistrationBuilder` | Global (all-handler) behavior configuration, applied in registration order. |
| `ForCommand<TCommand>()` | `IHandlerBehaviorScope` | Opens a per-handler behavior scope for the given command request type. Behaviors added here sit closest to the handler (innermost) by default. |
| `ForQuery<TQuery>()` | `IHandlerBehaviorScope` | Opens a per-handler behavior scope for the given query request type. Behaviors added here sit closest to the handler (innermost) by default. |
| `HandlerRegistrations` | `IReadOnlyList<HandlerRegistrationInfo>` | The handler registrations collected so far (post-dedupe; one entry per distinct service type), including the `RegistrationSource` that produced each one. Useful for diagnostics and tooling that need to answer "how did this handler get registered?" |
| `ValidatorRegistrations` | `IReadOnlyList<ValidatorRegistrationInfo>` | The validator registrations collected so far (post-dedupe), including the `RegistrationSource` that produced each one. |

## IHandlerRegistrationBuilder

Fluent sub-builder for registering handlers and validators, either explicitly by type, by assembly scanning, or via `IFeature` discovery. Handlers and validators are registered with a Scoped lifetime. All methods return the same builder for chaining.

| Method | Type Constraint | Description |
|---|---|---|
| `AddCommandHandler<THandler>()` | `THandler : class` | Registers a specific command handler implementing `ICommandHandler<TCommand, TResult>`. |
| `AddQueryHandler<THandler>()` | `THandler : class` | Registers a specific query handler implementing `IQueryHandler<TQuery, TResult>`. |
| `AddValidator<TValidator>()` | `TValidator : class` | Registers a specific validator implementing `IValidator<T>` or `IAsyncValidator<T>`. |
| `RegisterFromAssembly(Assembly assembly)` | - | Scans the given assembly, discovering `IFeature` implementers first (running their registration), then registering any remaining handlers and validators found. |
| `RegisterFromAssemblies(params Assembly[] assemblies)` | - | Scans multiple assemblies. See `RegisterFromAssembly`. |

## IBehaviorRegistrationBuilder

Fluent sub-builder for configuring global (all-handler) pipeline behaviors. Behaviors are applied as decorators around handlers in registration order (outermost first) and are resolved as singletons. All methods return the same builder for chaining.

| Method | Description |
|---|---|
| `Add(Type openGenericBehaviorType)` | Adds a global custom behavior by its open generic type definition (for example `typeof(MyBehavior<,>)`). The type must implement `IPipelineBehavior<TRequest, TResult>`. |
| `AddLogging()` | Adds the built-in logging behavior (request name and outcome). |
| `AddTiming()` | Adds the built-in timing behavior (elapsed time around the handler). |
| `AddExceptionToResult()` | Adds the built-in behavior that maps unexpected exceptions to a failure result. |
| `AddValidation()` | Adds the built-in validation behavior that runs registered validators and short-circuits on validation errors. |

## IHandlerBehaviorScope

Fluent scope for configuring behaviors on a single handler, opened via `ForCommand<TCommand>()` or `ForQuery<TQuery>()`. Behaviors added here sit closest to the handler (innermost) by default, inside any global behaviors. The placement modifiers apply to the next behavior added. All methods return the same scope for chaining.

| Method | Description |
|---|---|
| `Add(Type openGenericBehaviorType)` | Adds a per-handler custom behavior by its open generic type definition, using the currently pending placement (innermost by default). The type must implement `IPipelineBehavior<TRequest, TResult>`. |
| `AddLogging()` | Adds the built-in logging behavior for this handler. |
| `AddTiming()` | Adds the built-in timing behavior for this handler. |
| `AddExceptionToResult()` | Adds the built-in exception-to-result behavior for this handler. |
| `AddValidation()` | Adds the built-in validation behavior for this handler. |
| `AtOutermost()` | Places the next added behavior outside all existing behaviors for this handler (runs first on the way in). |
| `InsertBefore(Type anchorOpenGenericBehaviorType)` | Places the next added behavior immediately outside the given anchor behavior in this handler's pipeline. |

## RegistrationSource

`enum RegistrationSource` identifies how a handler or validator registration was discovered or added, so tooling and diagnostics can answer "how did this registration get here?"

| Value | Description |
|---|---|
| `Feature` | Registered via an `IFeature`'s `Register` call (phase 1 discovery). |
| `Scan` | Discovered by the loose assembly scan (phase 2 discovery). |
| `Manual` | Registered explicitly via `IHandlerRegistrationBuilder` (e.g. `AddCommandHandler<THandler>()`, `AddQueryHandler<THandler>()`, `AddValidator<TValidator>()`). |
| `Generated` | Reserved for a future source-generated registration path. |

## HandlerRegistrationInfo

`sealed record HandlerRegistrationInfo(Type ServiceType, Type ImplementationType, Type RequestType, Type ResultType, bool IsCommand, RegistrationSource Source)` is a read-only snapshot of a discovered handler registration, exposed via `IVerticallyBuilder.HandlerRegistrations`.

| Member | Type | Description |
|---|---|---|
| `ServiceType` | `Type` | The closed handler service interface (e.g. `ICommandHandler<TCommand, TResult>`). |
| `ImplementationType` | `Type` | The concrete handler implementation type. |
| `RequestType` | `Type` | The command or query request type. |
| `ResultType` | `Type` | The handler's result type. |
| `IsCommand` | `bool` | `true` for a command handler; `false` for a query handler. |
| `Source` | `RegistrationSource` | How this registration was discovered/added. |

## ValidatorRegistrationInfo

`sealed record ValidatorRegistrationInfo(Type ServiceType, Type ImplementationType, RegistrationSource Source)` is a read-only snapshot of a discovered validator registration, exposed via `IVerticallyBuilder.ValidatorRegistrations`.

| Member | Type | Description |
|---|---|---|
| `ServiceType` | `Type` | The closed validator service interface (e.g. `IValidator<TRequest>`). |
| `ImplementationType` | `Type` | The concrete validator implementation type. |
| `Source` | `RegistrationSource` | How this registration was discovered/added. |

## VerticallyDiagnostics

`static class VerticallyDiagnostics` provides on-demand debugging helpers that render the handler/validator registrations collected by an `IVerticallyBuilder`, including their `RegistrationSource`. Intended for ad-hoc diagnostics (console/log output), not as an always-on feature.

| Method | Return Type | Description |
|---|---|---|
| `PrintRegistrations(IVerticallyBuilder builder)` | `string` | Builds a formatted table of all collected handler and validator registrations, including the request/command type, the handler/validator implementation, and the `RegistrationSource` that produced each entry. Throws `ArgumentNullException` when `builder` is `null`. |
| `PrintDuplicateRegistrations(IVerticallyBuilder builder)` | `string` | Scans the collected handler and validator registration attempts for overlap: the same service type registered more than once (e.g. once by a feature and again by the loose assembly scan). Throws `ArgumentNullException` when `builder` is `null`, and `ArgumentException` when `builder` is not the internal `VerticallyBuilder` implementation. |
