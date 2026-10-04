# D20Tek.Vertically — Feature Group Design & Plan

> Status: Approved design, pending implementation
> Scope: Add an `IFeatureGroup` composition unit that bundles features and applies shared
> conventions (behaviors, metadata) to all of them, mirroring Minimal API's `MapGroup`.
> Keep `IFeature` unchanged (atomic, single-slice). Keep the global behavior layer as the
> always-on "middleware" analog.
> Constraint: Core stays UI-agnostic, low-magic, explicit-first, AOT/trim-friendly (no runtime
> reflection on the happy path).
> Target: **`net9.0` + `net10.0`**, Central Package Management, MSTest.

---

## 1. Motivation

Multi-slice features currently repeat per-handler behavior wiring, and there is no first-class
way to apply a shared pipeline/convention to a family of related slices. Minimal API solves the
same problem with `MapGroup`: a convention-bearing container that wraps individual `MapPost`/
`MapGet` endpoints and cascades filters/metadata to all of them.

This design adds the equivalent to Vertically without disturbing the existing single-slice
`IFeature` model:

- `IFeature` stays the atomic unit (one command/query + handler + validator), 1:1 with a handler.
- `IFeatureGroup` becomes the composition unit that includes features and cascades conventions.
- The global behavior layer (`builder.Behaviors`) is retained and reframed as the always-on
  "middleware" analog, distinct from group conventions.

---

## 2. Minimal API Analogy

| Minimal API | Vertically |
| --- | --- |
| `app.UseX()` middleware (always-on, every request) | `builder.Behaviors` global layer (always-on, every handler) |
| `app.MapGroup("/x")` (convention-bearing container) | `IFeatureGroup` (cascades behaviors/metadata to members) |
| `app.MapPost(...)` (single endpoint) | `IFeature` / `ForCommand<T>` / `ForQuery<T>` (single slice) |
| `.AddEndpointFilter<T>()` | per-handler behavior scope `Add(...)` / `AddValidation()` etc. |

Key insight: Minimal API did **not** eliminate the app-wide layer when it introduced groups; it
split app-wide concerns into **middleware** (universal) and **groups** (families). Vertically's
global behavior scope is the middleware analog and is kept for the same reason.

---

## 3. Key Design Decisions (with rationale)

### 3.1 `IFeature` stays unchanged and atomic
- **Decision:** Do not add group/cascade semantics to `IFeature`. It remains a single,
  self-registering slice, conventionally 1:1 with a handler.
- **Why:** Preserves the "one slice = one file" identity and keeps features decoupled - a
  feature does not know whether it is registered standalone or inside a group.

### 3.2 Add `IFeatureGroup` as the sole cascading-convention container
- **Decision:** Introduce `IFeatureGroup` whose `Configure` method includes member features and
  applies shared behaviors/metadata that cascade to every included feature.
- **Why:** Quarantines the only "magic" (implicit cascade) to one explicit, opt-in type. This
  respects the "low-magic, explicit-first" principle far better than sprinkling cascade onto
  every feature. Mirrors `MapGroup`, a vocabulary users already know.

### 3.3 Composition, not inheritance
- **Decision:** A group **references** features (`Include<TFeature>()`); features do not
  reference groups.
- **Why:** Correct dependency direction. A feature can be registered standalone or inside a
  group with no code change, and stays ignorant of grouping.

### 3.4 Keep the global behavior layer (do not remove it)
- **Decision:** Retain `builder.Behaviors` as the always-on layer for universal behaviors
  (logging, timing, exception-to-result, validation). Reframe/document it as the "middleware"
  analog, distinct from group conventions. (Optionally rename or clearly document; a
  `builder.RootGroup()` sugar may be added later on top of global, never instead of it.)
- **Why:** Removing global and forcing "apply to all" through a root group creates a silent-gap
  hazard: with assembly scanning, a scanned-but-ungrouped handler would bypass behaviors intended
  to be universal. That is exactly the "works until it doesn't" failure the library's
  explicit-first philosophy avoids. Global guarantees universal behaviors apply regardless of how
  a handler was registered.

### 3.5 Deterministic composition order
- **Decision:** Behaviors compose outermost -> innermost as:
  **global -> group -> per-handler**. Within each level, registration order is preserved
  (outer added first). Placement modifiers (`AtOutermost`, `InsertBefore`) act within their level.
- **Why:** One documented, predictable rule. Cascade only exists when a group is used.

### 3.6 Unified behavior convention vocabulary
- **Decision:** Group configuration reuses the same fluent behavior vocabulary as the per-handler
  scope (`AddLogging`, `AddTiming`, `AddValidation`, `AddExceptionToResult`, `Add(typeof(X<,>))`,
  `AtOutermost`, `InsertBefore`). Ideally collapse `IBehaviorRegistrationBuilder` and
  `IHandlerBehaviorScope` into one convention interface that global, group, and per-handler scopes
  all implement.
- **Why:** Users learn one vocabulary; net reduction in public surface despite added capability.

---

## 4. Proposed Public Surface

```csharp
// src/D20Tek.Vertically/IFeatureGroup.cs
public interface IFeatureGroup
{
	void Configure(IFeatureGroupBuilder builder);
}
```

```csharp
// src/D20Tek.Vertically/Registration/IFeatureGroupBuilder.cs
public interface IFeatureGroupBuilder
{
	// Membership (explicit, AOT-safe).
	IFeatureGroupBuilder Include<TFeature>() where TFeature : IFeature, new();
	IFeatureGroupBuilder Include(IFeature feature);
	IFeatureGroupBuilder IncludeFromAssembly(Assembly assembly);   // scan-based, opt-in

	// Cascading behavior conventions (shared vocabulary).
	IFeatureGroupBuilder AddLogging();
	IFeatureGroupBuilder AddTiming();
	IFeatureGroupBuilder AddValidation();
	IFeatureGroupBuilder AddExceptionToResult();
	IFeatureGroupBuilder Add(Type openGenericBehaviorType);
	IFeatureGroupBuilder AtOutermost();
	IFeatureGroupBuilder InsertBefore(Type anchorOpenGenericBehaviorType);

	// Optional metadata that member behaviors can read.
	IFeatureGroupBuilder WithName(string name);
	IFeatureGroupBuilder WithTags(params string[] tags);
}
```

Example usage (a family of user slices), with `CreateUser` unchanged from today:

```csharp
public sealed class UserFeatures : IFeatureGroup
{
	public void Configure(IFeatureGroupBuilder group) =>
		group.AddValidation()          // cascades to every included feature
			 .AddTiming()
			 .Include<CreateUser>()
			 .Include<GetUsers>();
}
```

---

## 5. Discovery / Registration Mechanics

Extends the existing two-phase scan ("features first, loose scan second") into three phases,
each skipping already-registered `(serviceType, implementationType)` pairs via the existing
dedupe `HashSet`.

1. **Groups first.** Discover `IFeatureGroup` implementers, instantiate (parameterless ctor),
   run `Configure`. Each group registers its included features and wraps them with the group's
   cascaded behaviors.
2. **Ungrouped features second.** Discover `IFeature` implementers **not owned by any group**;
   register them (as today).
3. **Loose scan third.** Discover remaining `ICommandHandler`/`IQueryHandler`/`IValidator` types
   not already registered and not owned by a feature/group.

Rules to nail down:

- **Group-vs-loose skip rule:** a feature included in a group is skipped by both the ungrouped-
  feature phase and the loose scan, so the group's cascaded behaviors are never bypassed. This is
  the correctness-critical rule; the group registration is authoritative.
- **Feature-in-multiple-groups policy (v1):** **forbid** - throw a clear exception at
  registration (fail fast), consistent with the existing "different implementation for an
  already-registered handler throws" rule. No last-wins.
- **Duplicate group/feature registration:** re-registering the same `(service, implementation)`
  pair remains a no-op (dedupe), preserving safe overlap between explicit and scan-based paths.
- **AOT/trim:** `Include<TFeature>()` uses explicit generic registration; `Configure` bodies must
  avoid nested assembly scans (same guidance already given for `IFeature.Register`). The
  scan-based `IncludeFromAssembly` is an opt-in convenience, not the AOT path.

---

## 6. Behavior Composition Model

For a handler registered via a group, the effective decorator chain (outermost first) is:

```
global behaviors  ->  group behaviors  ->  per-handler behaviors  ->  handler
```

- Each level preserves its own registration order.
- `AtOutermost` / `InsertBefore` reposition within the level where they are declared.
- A handler not in any group simply omits the group layer: `global -> per-handler -> handler`.

---

## 7. Tradeoffs

**Better (why this wins):**
- Preserves the atomic `IFeature` identity and "one slice = one file" model.
- Isolates cascade "magic" to a single explicit, opt-in `IFeatureGroup` type.
- Clean, familiar `MapGroup` analogy; features stay decoupled and composable.
- Reuses existing dedupe/ordering machinery with a modest three-phase extension.
- Keeps the guaranteed universal (global/middleware) layer, avoiding scan-gap bugs.

**Costs (accepted):**
- A third registration entry point (explicit `AddCommandHandler`, `IFeature`, `IFeatureGroup`) -
  more to document; mitigated by the Minimal API analogy.
- Two ways to express behaviors (group cascade vs. per-handler) - users must learn the
  composition order (Section 6).
- Reading a single feature no longer reveals its full pipeline; you must also inspect its group -
  the same accepted cost `MapGroup` carries.

---

## 8. Out of Scope (this pass)

- Renaming `builder.Behaviors` (kept as-is; only reframed/documented as the middleware layer).
- `builder.RootGroup()` sugar - may be added later **on top of** global, never instead of it.
- Nested feature groups (groups containing groups) - revisit only if a concrete need appears.
- Source-generator emission of group/feature registration - reuses the same public seams in a
  later wave.

---

## 9. Implementation Plan (Steps)

1. Add `IFeatureGroup` interface in `src/D20Tek.Vertically`.
2. Add `IFeatureGroupBuilder` interface in `Registration/` with membership + convention +
   metadata members.
3. (Optional but recommended) Unify `IBehaviorRegistrationBuilder` and `IHandlerBehaviorScope`
   behavior vocabulary into a shared convention interface reused by the group builder.
4. Implement `FeatureGroupBuilder` (concrete) that records membership and pending conventions.
5. Extend the decorator composer to apply the global -> group -> per-handler layering.
6. Extend discovery to the three-phase scan (groups -> ungrouped features -> loose), wiring the
   group-vs-loose skip rule into the existing dedupe `HashSet`.
7. Implement the feature-in-multiple-groups fail-fast check.
8. Add MSTest coverage: cascade ordering, group-vs-loose skip, multi-group throw, duplicate
   dedupe no-op, standalone feature still works unchanged.
9. Update a sample (e.g., group the IssueTracker user slices under an `IFeatureGroup`).
10. Update `docs/` api-reference and `CHANGELOG.md` (Added) per the workspace documentation rules.
11. Build + run tests to validate.
