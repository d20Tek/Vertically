# API Reference: Pipeline and Behaviors

This document covers the pipeline abstractions in the `D20Tek.Vertically.Pipeline` namespace and the built-in behaviors in the `D20Tek.Vertically.Behaviors` namespace. Behaviors are opt-in decorators that wrap the execution of a command or query handler.

See the [API Reference hub](api-reference.md) for the other reference documents.

## Table of Contents

- [Pipeline Contracts](#pipeline-contracts)
- [Delegates](#delegates)
- [Built-in Behaviors](#built-in-behaviors)

## Pipeline Contracts

| Interface | Type Parameters | Member | Return Type | Description |
|---|---|---|---|---|
| `IPipelineBehavior<TRequest, TResult>` | `TRequest : notnull`, `TResult : notnull` | `HandleAsync(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct)` | `Task<Result<TResult>>` | The single contract implemented by both built-in and custom behaviors. Behaviors are composed around the handler in registration order (outermost first). A behavior may short-circuit the pipeline by returning a `Result<TResult>` failure without invoking `next`. |
| `IScopedBehavior` | - | (marker) | - | Opt-in marker for behaviors that require a scoped lifetime because they resolve scoped services (for example `IValidator<T>`) from the request's `IServiceProvider`. Behaviors are registered as singletons by default; implementing this marker causes registration to use a scoped lifetime instead, avoiding a captive-dependency bug. |

## Delegates

| Delegate | Type Parameters | Return Type | Description |
|---|---|---|---|
| `RequestHandlerDelegate<TResult>()` | `TResult : notnull` | `Task<Result<TResult>>` | The continuation of a request pipeline: invoking it either calls the next behavior in the chain or, at the innermost position, the actual handler. |

## Built-in Behaviors

Each built-in behavior is an open generic class implementing `IPipelineBehavior<TRequest, TResult>` with `where TRequest : notnull` and `where TResult : notnull`. They are added through the registration builders (see [Registration](api-reference-registration.md)) rather than constructed directly.

| Behavior | Lifetime | Description |
|---|---|---|
| `LoggingBehavior<TRequest, TResult>` | Singleton | Logs the start and outcome (success or failure) of each request. The logger is optional: when no `ILogger<T>` is registered, it falls back to `NullLogger<T>` and continues without emitting logs. Added via `AddLogging()`. |
| `TimingBehavior<TRequest, TResult>` | Singleton | Measures the elapsed time of the downstream pipeline (behaviors plus handler) and logs it. Kept separate from `LoggingBehavior` so timing can be enabled independently. Falls back to `NullLogger<T>` when no logger is registered. Added via `AddTiming()`. |
| `ValidationBehavior<TRequest, TResult>` | Scoped (implements `IScopedBehavior`) | Runs registered `IValidator<T>` and `IAsyncValidator<T>` instances for the request and short-circuits to a validation failure result when any validator reports errors. Only runs when validators are registered. Resolves validators lazily from the request scope to avoid a captive-dependency bug. Added via `AddValidation()`. |
| `ExceptionToResultBehavior<TRequest, TResult>` | Singleton | Catches unexpected exceptions from the downstream pipeline and maps them to a failure `Result<TResult>` (an `Unexpected` error) so the Result contract holds end-to-end. Cancellation (`OperationCanceledException`) is intentionally allowed to propagate. Added via `AddExceptionToResult()`. |
