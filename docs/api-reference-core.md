# API Reference: Core Abstractions

This document covers the core contracts in the `D20Tek.Vertically` namespace. These are the types you implement when authoring a vertical slice: the request marker interfaces, the handler interfaces, the validator interfaces, and the optional self-registering feature contract.

See the [API Reference hub](api-reference.md) for the other reference documents.

## Table of Contents

- [Request Interfaces](#request-interfaces)
- [Handler Interfaces](#handler-interfaces)
- [Validator Interfaces](#validator-interfaces)
- [Feature Interface](#feature-interface)

## Request Interfaces

A request carries its result type so that handlers, behaviors, and registration can discover the request-to-result pairing from the request alone.

| Interface | Type Parameters | Description |
|---|---|---|
| `ICommand<TResult>` | `TResult : notnull` | Marker for a command that, when handled, produces a result of type `TResult`. Use for operations that change state. |
| `IQuery<TResult>` | `TResult : notnull` | Marker for a query that, when handled, produces a result of type `TResult`. Use for read operations. |

## Handler Interfaces

Handlers implement the logic for a single request and return a `Result<TResult>`. You inject the closed handler interface directly into presentation code and call `HandleAsync`.

| Interface | Type Parameters | Member | Return Type | Description |
|---|---|---|---|---|
| `ICommandHandler<TCommand, TResult>` | `TCommand : ICommand<TResult>`, `TResult : notnull` | `HandleAsync(TCommand command, CancellationToken ct)` | `Task<Result<TResult>>` | Handles the specified command asynchronously and returns a `Result` wrapping the command result. |
| `IQueryHandler<TQuery, TResult>` | `TQuery : IQuery<TResult>`, `TResult : notnull` | `HandleAsync(TQuery query, CancellationToken ct)` | `Task<Result<TResult>>` | Handles the specified query asynchronously and returns a `Result` wrapping the query result. |

## Validator Interfaces

Validators run before the handler when the validation behavior is enabled. Implement the synchronous contract for in-memory checks and the asynchronous contract when validation requires I/O or remote calls.

| Interface | Type Parameters | Member | Return Type | Description |
|---|---|---|---|---|
| `IValidator<T>` | `T : notnull` | `Validate(T input)` | `ValidationErrors` | Validates the input synchronously and returns any validation errors. |
| `IAsyncValidator<T>` | `T : notnull` | `ValidateAsync(T input, CancellationToken ct)` | `Task<ValidationErrors>` | Validates the input asynchronously and returns any validation errors. |

## Feature Interface

A feature bundles a slice's command or query, handler, validator, and any slice-specific service registrations into a single self-registering unit. Implementers are discovered during assembly scanning (before the loose handler scan), instantiated via their parameterless constructor, and asked to register themselves.

| Interface | Member | Return Type | Description |
|---|---|---|---|
| `IFeature` | `Register(IVerticallyBuilder builder)` | `void` | Registers this feature's handlers, validators, behaviors, and services against the supplied builder. Because static classes cannot implement interfaces, a feature is a non-static class whose `Command`/`Handler`/`Validator` types may still be nested inside it. For AOT and trim safety, use explicit generic registration and avoid nested assembly scans. |
