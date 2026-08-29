# D20Tek.Vertically — Release 1 Design & Plan

> Status: Approved design, pending implementation
> Package: `D20Tek.Vertically` — interface definitions and helper classes for writing
> Vertical Slice Architecture (VSA) across app types: Blazor (web), WebApi (Minimal API),
> CLI, and (future) WPF/Avalonia.
> Constraint: The core library must **not** depend on any UI framework.
> Core dependencies (allowed, UI-agnostic): `D20Tek.Functional`,
> `Microsoft.Extensions.DependencyInjection.Abstractions`,
> `Microsoft.Extensions.Logging.Abstractions`.
> Target: **`net9.0` + `net10.0`** (multi-target, matching sibling packages),
> Central Package Management, MSTest.

---

## 1. Philosophy & Identity

`D20Tek.Vertically` is deliberately **different from mediator-style libraries** (MediatR, etc.).
Its guiding principles:

- **Low-magic, explicit-first.** Direct handler injection is the blessed path.
- **AOT-friendly.** No runtime reflection required on the happy path; reflection-based
  scanning is opt-in convenience only.
- **No sender/dispatcher** in release 1. The indirection of `ISender.Send(request)` that
  trips people up (loses F12 navigation, moves failures from compile-time to runtime) is
  intentionally omitted.
- **Cross-cutting concerns are opt-in decorators**, not a mandatory pipeline. Most
  developers don't use them, so they should never be forced to pay for them.
- **Result-oriented.** Handlers return `Result<T>` (from `D20Tek.Functional`). Expected
  failures are Results, not exceptions.

VSA is about organizing code by feature slice; how a slice is *invoked* is orthogonal.
This library provides the slice contracts + a clean registration story, not a mediator clone.

---

## 2. Key Design Decisions (with rationale)

### 2.1 No dispatcher/sender (Release 1)
- **Decision:** Handlers are injected and called directly. No `ISender`/`IMediator`.
- **Why:** Less indirection, compile-time safety, natural AOT/trim friendliness, honest
  dependencies. The main reason mediators exist (pipeline behaviors) is delivered instead
  via decorators, which work with direct injection.
- **Wave 2:** An optional thin `ISender` convenience layer *may* be added later if demand
  appears — but it will never be the backbone.

### 2.2 Behaviors are decorator-based
- **Decision:** Cross-cutting concerns wrap the handler interface as decorators, so they
  run even when a consumer injects `ICommandHandler<,>` directly. Opt-in.
- **Why:** Delivers the #1 value of a mediator (cross-cutting concerns) without imposing
  the indirection. Consistent with "direct is blessed, magic is optional."

### 2.3 Requests carry their result type
- **Decision:** `ICommand<TResult>` and `IQuery<TResult>` **only** — no non-generic base
  markers. Handlers constrained:
  `ICommandHandler<TCommand,TResult> where TCommand : ICommand<TResult>`.
- **Why:** Self-describing requests make scanning, decorator composition, and (especially)
  the wave-2 source generator dramatically simpler and safer. Cost to the user is just
  `: ICommand<OrderId>` on the declaration.
- **No non-generic `ICommand`/`IQuery` base markers (option 2).** Rationale:
  - C# forbids a public interface inheriting an internal one, so an "internal base carried
    publicly" is impossible — the choice is binary: public base marker, or no base marker.
  - A base marker only bought a cheap runtime-scan pre-filter
    (`IsAssignableFrom(typeof(ICommand))`); scanning instead matches on the open generic
    `ICommand<>` / `IQuery<>`, which is trivial and done once at startup.
  - The source generator keys off the **generic type arguments** of handler interfaces via
    the semantic model, so a base marker gives it no benefit either.
  - Net: removing the base markers yields the smallest, most intentional public surface with
    no meaningful complexity cost to either the scanner or the generator.
- **Bug fix included:** `IQuery` was previously declared `public class IQuery` — now an
  `interface` (already corrected in the codebase).

### 2.4 Single behavior contract for built-ins AND custom behaviors
- **Decision:** One `IPipelineBehavior<TRequest,TResult>` with a `next`-delegate:
  ```csharp
  public delegate Task<Result<TResult>> RequestHandlerDelegate<TResult>();

  public interface IPipelineBehavior<TRequest, TResult>
	  where TRequest : notnull
	  where TResult : notnull
  {
	  Task<Result<TResult>> HandleAsync(
		  TRequest request,
		  RequestHandlerDelegate<TResult> next,
		  CancellationToken ct = default);
  }
  ```
- **Why:** Uniform and simple for custom behaviors (write one `HandleAsync`, no need to
  re-implement handler shapes or manage inner-handler injection). One internal adapter
  bridges the behavior chain into the actual handler-interface decorator DI resolves.
  Result-aware, so behaviors can short-circuit by returning a failure without calling
  `next()`. Commands and queries share the same behavior contract since both funnel through
  `Result<TResult>`.
- **Lifetime:** Behaviors are **stateless singletons** (they receive request + `next` each
  call). A behavior needing scoped deps is the exception, not the default.

### 2.5 Registration surface
- **Decision:** Single branded entry point `services.AddVertically(builder => { ... })`.
- **Why `AddVertically` over `AddVerticalSlices`:** Reads naturally, reinforces the brand,
  best IntelliSense discoverability (`AddVert...`). Descriptive intent lives in the builder
  methods, so no clarity is lost.
- **Grouped sub-objects on the builder.** The top-level builder exposes two capability
  groups rather than a flat method soup:
  - `builder.Handlers` — handler registration (explicit + scanning).
  - `builder.Behaviors` — global behavior configuration.
  This keeps the root builder's IntelliSense showing *categories*, avoids generic-verb
  collisions (`AddLogging`, `AddValidation`), and gives each group room to grow settings later.
- **Per-handler behaviors stay direct on the `ForCommand<T>()`/`ForQuery<T>()` scope
  (option B)** — that scope is already narrowed to one handler and behaviors are essentially
  the only thing configured there, so a nested `.Behaviors` would be redundant ceremony.
- **Supports both** explicit registration and assembly scanning (source-generator-based
  registration is wave 2):
  ```csharp
  services.AddVertically(builder =>
  {
	  // Handler registration — grouped under Handlers:
	  builder.Handlers.RegisterFromAssembly(typeof(CreateOrder).Assembly); // scanning
	  builder.Handlers.AddCommandHandler<CreateOrderHandler>();            // explicit
	  builder.Handlers.AddQueryHandler<GetOrderHandler>();

	  // Global behaviors — grouped under Behaviors, applied in registration order (outer -> inner):
	  builder.Behaviors
			 .AddLogging()
			 .AddTiming()
			 .AddExceptionToResult()
			 .AddValidation()
			 .AddBehavior<MyCustomBehavior>();

	  // Per-handler behaviors — direct on the scope (option B), innermost by default:
	  builder.ForCommand<PlaceOrder>().AddTiming();
	  builder.ForCommand<PlaceOrder>().InsertBefore<LoggingBehavior>().AddTiming(); // override
  });
  ```

### 2.6 Behavior ordering
- **Model:** The pipeline is an ordered list from **outermost -> innermost (handler)**.
  Registration order = wrapping order. The first-registered behavior runs first on the way
  in and last on the way out. `next()` calls inward.
  ```
  [ b0 ][ b1 ][ b2 ]( handler )
	outer ............ inner
  ```
- **Global order:** registration order.
- **Per-handler order:** per-handler behaviors sit **closest to the handler** (innermost),
  inside the global ones, by default. Deterministic and intuitive.
  ```
  builder.Behaviors.AddLogging().AddExceptionToResult();   // globals
  builder.ForCommand<PlaceOrder>().AddTiming();             // per-handler
  // PlaceOrder pipeline:
  [Logging][ExceptionToResult][Timing](PlaceOrderHandler)
  ```
- **Override:** `InsertBefore<T>()` / a `Placement` option (e.g., `Placement.Outermost`)
  for the cases where precise position matters. Default stays zero-thought.
- **Consequence:** Because per-handler pipelines differ per request type, the decorator
  registration is built **per closed handler type** — enumerate registered handlers, compute
  each one's ordered behavior list, register the composed decorator chain for that specific
  `ICommandHandler<TCommand,TResult>`. Sets up cleanly for wave-2 source-generated chains.

### 2.7 Open-generic decorator registration (no Scrutor)
- **Decision:** No Scrutor dependency. Hand-code the open-generic decorator registration
  against `IServiceCollection`, composing chains per closed handler type. We own a generic
  `BehaviorHandlerDecorator<TRequest,TResult>` adapter that turns the `IPipelineBehavior`
  chain into the `ICommandHandler`/`IQueryHandler` decorator that DI resolves.

### 2.8 Validation stays explicit
- **Decision:** `IValidator<T>` (existing) is never silently auto-injected. The provided
  **Validation behavior** runs `IValidator<TRequest>` when one is registered, and/or the
  handler validates inline. Both paths are explicit.

### 2.9 Built-in behaviors (all replaceable via DI)
1. **Logging** — request name + outcome (success/failure).
2. **Timing** — stopwatch around the handler (kept separate from Logging).
3. **ExceptionToResult** — catch unexpected exceptions and map to a `Result` failure so the
   Result contract holds end-to-end.
4. **Validation** — runs `IValidator<TRequest>` when registered; short-circuits to a failure
   `Result` on validation errors.

### 2.10 Optional `IFeature` — self-registering slice unit

- **Decision:** Add an **optional** `IFeature` (shape A, instance-based) to core so a slice can
  bundle its command/query, handler, validator, and any slice-specific DI into one
  self-registering unit. Purely additive — developers who prefer the static nested-class
  convention or plain explicit/scanned registration are unaffected.
  ```csharp
  public interface IFeature
  {
      void Register(IVerticallyBuilder builder);
  }

  public sealed class CreateOrder : IFeature
  {
      public void Register(IVerticallyBuilder builder)
      {
          builder.Handlers.AddCommandHandler<Handler>();
          builder.ForCommand<Command>().AddTiming();
      }

      public sealed record Command(/* ... */) : ICommand<OrderId>;
      public sealed class Validator : IValidator<Command> { /* ... */ }
      public sealed class Handler : ICommandHandler<Command, OrderId> { /* ... */ }
  }
  ```
- **Static-class caveat:** static classes can't implement interfaces, so a feature that
  implements `IFeature` is a **non-static** class (nested `Command`/`Handler`/`Validator`
  still allowed). The conventional static nested-class remains available for those who don't
  want `IFeature`.
- **Discovery is part of the same scan** (two-phase, deterministic order):
  1. **Features first** — discover `IFeature` implementers, instantiate (parameterless ctor),
     run each `Register`.
  2. **Loose scan second** — discover `ICommandHandler`/`IQueryHandler`/`IValidator` types and
     register only those **not already registered**, and **skip types nested inside an
     `IFeature`** (feature-owned).
- **Source-generator compatibility:** the `Register` body is ordinary reflection-free C#
  (explicit generic registrations), so it is AOT/trim-safe as-is. The wave-2 generator only
  replaces the **reflection discovery** of `IFeature` types with emitted
  `new TFeature().Register(builder)` calls. **Guidance:** features must use explicit generic
  registration inside `Register` (never a nested `RegisterHandlersFromAssembly`, which would
  reintroduce reflection and break AOT).

### 2.11 Samples — UI-agnostic
- **Decision:** Three samples (WebApi Minimal API, Blazor, CLI), each invoking the **same**
  handlers but mapping `Result<T>` differently, with the mapping written **by hand** in the
  sample:
  - **WebApi:** slice = static `Map` extension; inject handler, call `HandleAsync`, translate
	`Result<T>` -> `Results.Ok/Problem`.
  - **Blazor:** component/page injects handler, calls `HandleAsync`, binds `Result<T>` to UI
	state.
  - **CLI:** command class injects handler, maps `Result<T>` -> exit code + console output.
- **No platform integration packages yet** (`D20Tek.Vertically.AspNetCore`, `.Cli`, etc.).
  Revisit once patterns are proven, keeping the core UI-agnostic.

### 2.12 Operational conventions (lifetimes, void, errors, targeting)

- **DI lifetimes:** handlers **and** validators (`IValidator<T>`) are registered **Scoped**
  by default (matches ASP.NET request scope / EF `DbContext`; CLI & Blazor create a scope per
  operation). Behaviors remain **singletons**. Validators are auto-registered by scanning
  alongside handlers.
- **Void / no-result commands:** standardize on **`Result<Unit>`** (single handler shape, no
  second non-generic surface). `D20Tek.Functional` does not yet expose a `Unit` type — it
  will be **added to `D20Tek.Functional`** to support this API cleanly. `Unit` is a
  prerequisite dependency for the void-command ergonomics.
- **Duplicate / double-discovery policy:** registration tracks a
  `HashSet<(Type serviceType, Type implementationType)>`.
  - Re-registering the **same** (service, implementation) pair is a **no-op** (dedupe). This
    is what makes feature registration + loose scan safe together.
  - Registering a **different** implementation for an already-registered handler service
    (two handlers competing for the same `ICommand<TResult>` / `IQuery<TResult>`) **throws**
    a clear exception at registration (fail fast). No last-wins.
  - Discovery order: **features first, loose scan second**; the scan skips already-registered
    pairs and types nested inside `IFeature` implementers.
- **Missing handlers:** no eager validation pass in release 1. A missing handler simply fails
  DI resolution at the call site (acceptable given there is no sender). May add an optional
  eager check / analyzer later.
- **Error mapping:**
  - **ExceptionToResult behavior:** caught exceptions map to **`Error.Unexpected(exception)`**.
  - **Validation behavior:** validation failures return a `Result` failure with the `Error`
    `Code` set to **`Validation`**.
  - Both `Error.Unexpected` and the `Validation` error code are supported by
    `D20Tek.Functional`, so we map onto existing types rather than inventing new ones.
- **Target frameworks:** multi-target **`net9.0` + `net10.0`** (matching sibling D20Tek
  packages) for the core library, tests, and samples where applicable.

---

## 3. Out of Scope (Wave 2+)

- **Source generator** (`D20Tek.Vertically.Generators`, separate analyzer package) for
  scanning-free AOT registration and possibly behavior-chain emission. Reuses the same public
  registration seams.
- **Caching behavior** (needs a cache abstraction).
- **Optional `ISender`** convenience layer.
- **Notifications** (`INotification` + multiple handlers).
- **Streaming queries** (`IAsyncEnumerable<T>`).
- **Platform integration packages** for Result -> response mapping.

---

## 4. Proposed Repo / Package Shape

> **Layout note:** the library currently lives at the repo root (`D20Tek.Vertically/`). It
> will be **moved under `src/`** so `src/`, `tests/`, and `samples/` sit side-by-side at the
> solution root. The `.slnx` will be updated accordingly.

```
src/
  D20Tek.Vertically/                   # core abstractions + registration + built-in behaviors
	ICommand.cs                        # ICommand, ICommand<TResult>
	IQuery.cs                          # IQuery, IQuery<TResult>   (fix class -> interface)
	ICommandHandler.cs                 # constrained to ICommand<TResult>
	IQueryHandler.cs                   # constrained to IQuery<TResult>
	IValidator.cs                      # existing
	Pipeline/
	  IPipelineBehavior.cs             # behavior contract + RequestHandlerDelegate
	  Behaviors/
		LoggingBehavior.cs             # uses Microsoft.Extensions.Logging.Abstractions
		TimingBehavior.cs
		ExceptionToResultBehavior.cs   # Error.Unexpected(exception)
		ValidationBehavior.cs          # Error.Code = Validation
	Registration/
	  ServiceCollectionExtensions.cs   # AddVertically
	  VerticallyBuilder.cs             # root builder exposing Handlers + Behaviors groups + ForCommand/ForQuery
	  HandlerRegistrationBuilder.cs    # builder.Handlers — explicit registration + assembly scanning (Scoped)
	  BehaviorRegistrationBuilder.cs   # builder.Behaviors — global behavior configuration (Singleton)
	  HandlerDecoratorComposer.cs      # open-generic decorator wiring, per closed handler type

tests/
  D20Tek.Vertically.Tests/             # MSTest, mirrors source

samples/
  WebApi/                              # Minimal API, hand-written Result -> IResult
  Blazor/                             # Result -> UI state
  Cli/                                # Result -> exit code + console
```

> Core targets `net9.0;net10.0`. Depends on `D20Tek.Functional` (incl. forthcoming `Unit`),
> `Microsoft.Extensions.DependencyInjection.Abstractions`, and
> `Microsoft.Extensions.Logging.Abstractions`.

---

## 5. Implementation Plan (Steps)

0. [done] **Prerequisite (external):** add a `Unit` type to `D20Tek.Functional` and publish, so
   void commands can standardize on `Result<Unit>`. (Tracked separately in that repo.)
1. [done] Move the library project under `src/` and update the `.slnx`; multi-target `net9.0;net10.0`.
2. [done] Fix `IQuery` to an interface and add result-typed `ICommand<TResult>` / `IQuery<TResult>`
   markers.
3. [done] Tighten `ICommandHandler` / `IQueryHandler` constraints to the result-typed request markers.
4. [done] Define `IPipelineBehavior<TRequest,TResult>` and `RequestHandlerDelegate<TResult>`.
5. [done] Add the `Microsoft.Extensions.DependencyInjection.Abstractions` and
   `Microsoft.Extensions.Logging.Abstractions` references and package plumbing.
6. Implement the fluent builder: `VerticallyBuilder` with `Handlers` (explicit + scanning,
   Scoped handlers & validators) and `Behaviors` (Singleton) groups. And define IFeature interface.
7. Implement the open-generic handler decorator composer (per closed handler type, ordered
   chain; duplicate-handler registration throws).
8. Add global + custom behavior registration and per-handler `ForCommand`/`ForQuery` (option B)
   with `InsertBefore<T>()` / `Placement` overrides.
9. Implement built-in behaviors: Logging, Timing, ExceptionToResult (`Error.Unexpected`),
   Validation (`Error.Code = Validation`).
10. Add the `AddVertically` `IServiceCollection` extension tying the builder together.
11. Create MSTest project and cover contracts, registration, decorator ordering, and each
	behavior.
12. Create the WebApi (Minimal API) sample with hand-written `Result<T>` -> `IResult` mapping.
13. Create the Blazor sample binding `Result<T>` to UI state.
14. Create the CLI sample mapping `Result<T>` to console output / exit codes.
15. Build the solution and run tests to validate.

---

## 6. Risks & Open Questions

- **`Unit` prerequisite:** `Result<Unit>` ergonomics depend on `D20Tek.Functional` shipping a
  `Unit` type first; sequence that external change ahead of void-command work.
- **Per-handler pipeline composition per closed handler type** is the trickiest piece; must
  stay deterministic and AOT-friendly for the wave-2 generator.
- **Adapting the `next`-delegate contract into DI-resolved open-generic decorators** without
  Scrutor needs careful lifetime handling (singleton behaviors, scoped handlers/validators).
- **Placement override API** (`InsertBefore<T>()` / `Placement`) should stay minimal to avoid
  ordering foot-guns.
- **Project move to `src/`** must update `.slnx` references and any relative paths.

---

## 7. Decision Log (conversation summary)

| Topic | Decision |
|-------|----------|
| Dispatcher/sender | **None** in release 1; direct handler injection blessed path |
| Behaviors | **Decorator-based**, opt-in |
| Result type on request | **Yes** — `ICommand<TResult>` / `IQuery<TResult>` only (no non-generic base markers, option 2) |
| Behavior contract | Single `IPipelineBehavior<TRequest,TResult>` with `next` delegate |
| Behavior lifetime | **Singletons** (stateless) |
| Built-in behaviors | Logging, Timing, ExceptionToResult, Validation (all replaceable) |
| Validation | **Explicit** (behavior when validator registered, or in handler) |
| Registration entry | `services.AddVertically(builder => ...)` |
| Builder layout | Grouped sub-objects: `builder.Handlers` + `builder.Behaviors` |
| Handler registration | Under `builder.Handlers` (`AddCommandHandler<T>`, `RegisterFromAssembly`) |
| Global behaviors | Under `builder.Behaviors` (`AddLogging()`, `AddValidation()`, ...) |
| Registration modes | Explicit + assembly scanning (source gen = wave 2) |
| Behavior order | Registration order (outer -> inner) |
| Per-handler behaviors | Direct on `ForCommand<T>()` scope (option B); **innermost by default**, `InsertBefore<T>()` / `Placement` override |
| Custom behaviors | Supported via same contract + `AddBehavior<T>()` |
| Decorator registration | Hand-coded open-generic (no Scrutor) |
| Handler/validator lifetime | **Scoped** (behaviors stay Singleton) |
| Void commands | **`Result<Unit>`**; `Unit` to be added to `D20Tek.Functional` |
| Duplicate handlers | Same (service, impl) pair = no-op dedupe; **different** impl for same request = throw |
| IFeature | **Optional** `IFeature.Register(IVerticallyBuilder)` (shape A, instance); discovered in same scan (features first, then loose scan skipping owned/duplicate types) |
| Missing handlers | No eager validation in release 1; fails at DI resolution |
| ExceptionToResult mapping | **`Error.Unexpected(exception)`** |
| Validation mapping | `Result` failure with `Error.Code = Validation` |
| Core dependencies | `D20Tek.Functional`, DI.Abstractions, **Logging.Abstractions** |
| Target frameworks | **`net9.0;net10.0`** (multi-target) |
| Repo layout | `src/` + `tests/` + `samples/` at solution root (move library into `src/`) |
| Samples | 3 UI-agnostic (WebApi, Blazor, CLI); mapping by hand |
| Integration packages | **Not yet** |
| Source generator | **Wave 2**, separate analyzer package |
