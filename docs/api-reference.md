# API Reference

This is the API reference hub for D20Tek.Vertically. Because the library has a larger public surface than a single service, the reference is split into focused documents that mirror the library's namespaces. Start here, then follow the link for the area you are working in.

## Reference Documents

| Document | Namespace | Covers |
|---|---|---|
| [Core Abstractions](api-reference-core.md) | `D20Tek.Vertically` | The command, query, handler, validator, and feature contracts you implement in a slice. |
| [Registration](api-reference-registration.md) | `Microsoft.Extensions.DependencyInjection`, `D20Tek.Vertically.Registration` | The `AddVertically` entry point and the fluent builders for registering handlers, validators, and behaviors. |
| [Pipeline and Behaviors](api-reference-behaviors.md) | `D20Tek.Vertically.Pipeline`, `D20Tek.Vertically.Behaviors` | The pipeline behavior contracts and the built-in logging, timing, validation, and exception-to-result behaviors. |
| [Queries: Pagination, Sorting, and Filtering](api-reference-queries.md) | `D20Tek.Vertically.Queries.Pagination` | The paged request and result types, sort expressions, the filter tree, and their request validators. |

## Conventions

- All handlers return a `Result<TResult>` (from D20Tek.Functional), so expected failures are values rather than exceptions.
- Type parameters constrained with `where TResult : notnull` are noted in each table where relevant.
- Types marked `internal` are excluded from this reference; only the public surface is documented.
