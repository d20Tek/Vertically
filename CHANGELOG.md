# Changelog

All notable changes to D20Tek.Vertically are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.9.5]

### Added

- `<IsAotCompatible>true</IsAotCompatible>` on the `D20Tek.Vertically` package.
- Typed handler overloads `AddCommandHandler<THandler, TCommand, TResult>()` and
  `AddQueryHandler<THandler, TQuery, TResult>()` that compose pipeline behaviors without reflection.
- `Add<TBehavior>()` on `IBehaviorRegistrationBuilder` and `IHandlerBehaviorScope` for closed,
  trim- and AOT-compatible custom behaviors.
- `IHandlerRegistrationBuilder.AddFeature<TFeature>()` for explicit, AOT-compatible feature registration.
- `samples/IssueTracker/IssueTracker.WebAot` sample host that uses typed registrations instead of
  assembly scanning. The IssueTracker features now register handlers with the typed overloads.
- `samples/AotCli` minimal console sample that publishes as a fully trimmed Native AOT executable
  with no trim or AOT warnings, validating the typed registration and behavior composition paths.

### Changed

- Pipeline behavior composition now uses generic code instead of `MakeGenericType`,
  `Array.CreateInstance`, and reflection-based constructor calls. Built-in behaviors are AOT-safe
  on every registration path.
- `RegisterFromAssembly`, `RegisterFromAssemblies`, the single-type-argument `AddCommandHandler<THandler>()`
  and `AddQueryHandler<THandler>()`, and `Add(Type)` on behavior builders are annotated with
  `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`, so the trim and AOT analyzers flag them.
- `IHandlerBehaviorScope.InsertBefore` accepts closed behavior types as anchors; its parameter is
  renamed to `anchorBehaviorType`.

## [0.9.4]

### Added

- `RegistrationSource` enum tracking how each handler/validator registration was discovered
  (`Feature`, `Scan`, `Manual`, or `Generated`).
- `IVerticallyBuilder.HandlerRegistrations` and `IVerticallyBuilder.ValidatorRegistrations`,
  exposing the collected registrations (as `HandlerRegistrationInfo` / `ValidatorRegistrationInfo`)
  along with their `RegistrationSource` for diagnostics and tooling.
- `VerticallyDiagnostics` static class with `PrintRegistrations(...)` (formatted registration
  table dump) and `PrintDuplicateRegistrations(...)` (duplicate/overlap report) debug helpers.

### Changed

- Updated package dependencies to their latest versions.
- `samples/IssueTracker/IssueTracker.Api` now maps `Result<T>` to HTTP responses using the
  `D20Tek.Functional.AspNetCore` package's Minimal API extensions instead of a custom `ResultHttpExtensions` helper.

## [0.9.3] - 2026-09-11

### Changed

- Updated package dependencies to their latest versions.
- Switched the NuGet release workflow to trusted publishing, using OIDC to obtain a temporary
  nuget.org API key instead of a long-lived secret.

## [0.9.1] - 2026-09-02

Initial public release of D20Tek.Vertically, a vertical-slice engine for modern .NET
applications targeting net9.0 and net10.0.

### Added

- Core vertical-slice abstractions for commands, queries, and their handlers, plus the
  self-registering feature model used to group a slice's request, validator, and handler.
- Validation abstractions for synchronous and asynchronous validators.
- A pipeline model with IBehavior definition and built-in behaviors for logging, timing, validation, and
  exception-to-result translation, composable per handler.
- Dependency-injection registration, including assembly scanning to discover feature
  handlers and a builder for selecting pipeline behaviors as a host-owned policy.
- Query pagination support, including offset and cursor paged requests, sorting, and a
  composable filtering model, along with their request validators.
- Package documentation (README and a segmented API reference under docs/) and Source Link
  enabled symbol packages.

### Samples

- Issue Tracker sample suite demonstrating a single Application and Persistence layer
  (EF Core, SQLite) consumed by three hosts that share the same vertical slices:
  - A Minimal API host with RFC 7807 problem-details translation and an OpenAPI/Scalar UI.
  - A Blazor Web App host with interactive server rendering.
  - A command-line host built with System.CommandLine.

[0.9.2]: https://github.com/d20Tek/Vertically/releases/tag/v0.9.2
[0.9.1]: https://github.com/d20Tek/Vertically/releases/tag/v0.9.1
