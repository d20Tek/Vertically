# Changelog

All notable changes to D20Tek.Vertically are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[0.9.1]: https://github.com/d20Tek/Vertically/releases/tag/v0.9.1
