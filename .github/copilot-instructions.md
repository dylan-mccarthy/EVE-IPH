# Copilot Instructions — EVE Isk per Hour (EVE-IPH)

## Project Intent

EVE-IPH is being modernised from a monolithic VB.NET WinForms application (.NET Framework 4.6.1) to a clean, cross-platform .NET 8 application using Avalonia UI. The modernisation follows the **Strangler Fig pattern**: new domain libraries are built alongside the legacy codebase and replace it incrementally without breaking existing functionality.

> See [`docs/architecture-assessment.md`](../docs/architecture-assessment.md) for a full analysis of the current codebase.
> See [`docs/modernisation-plan.md`](../docs/modernisation-plan.md) for the phased migration plan.

---

## Repository Layout

```
EVE-IPH/
├── src/                          # New .NET 8 projects (do NOT reference the legacy .vbproj)
│   ├── EVE.IPH.Domain.Core/
│   ├── EVE.IPH.Domain.Characters/
│   ├── EVE.IPH.Domain.Market/
│   ├── EVE.IPH.Domain.Manufacturing/
│   ├── EVE.IPH.Domain.Reprocessing/
│   ├── EVE.IPH.Domain.ShoppingList/
│   ├── EVE.IPH.Domain.Industry/
│   ├── EVE.IPH.Domain.Assets/
│   ├── EVE.IPH.Infrastructure.ESI/
│   ├── EVE.IPH.Infrastructure.Data/
│   ├── EVE.IPH.Infrastructure.Settings/
│   └── EVE.IPH.UI.Avalonia/
├── tests/                        # xUnit test projects (one per domain library)
├── docs/                         # Architecture assessment and modernisation plan
├── EVE-IPH-Modern.sln            # New .NET 8 solution (legacy .sln untouched)
├── Directory.Build.props         # Shared MSBuild properties for all new projects
└── <legacy VB.NET files>         # DO NOT MODIFY — legacy app runs alongside new code
```

---

## Golden Rules

1. **Never modify the legacy VB.NET project** (`EVE Isk per Hour.vbproj` and its `.vb`/`.resx`/`.Designer.vb` files) until a domain has been fully extracted, tested, and verified in the new codebase.
2. **All new code is C# targeting `net8.0`**. Do not add VB.NET files to the new solution.
3. **No global mutable state**. Use dependency injection everywhere. No `static` fields that hold application state.
4. **Domain libraries have no knowledge of UI or infrastructure.** They depend only on `EVE.IPH.Domain.Core` interfaces. Infrastructure injects concrete implementations at startup.
5. **Every public method on a domain service must have unit tests** before it is considered complete.
6. **Nullable reference types are enabled** (`<Nullable>enable</Nullable>`). Do not suppress nullable warnings with `!` unless there is no alternative and a comment explains why.
7. **Treat warnings as errors** in CI. Fix all warnings rather than suppressing them.

---

## Dependency Rules

```
EVE.IPH.UI.Avalonia
    → EVE.IPH.Domain.*
    → EVE.IPH.Infrastructure.*

EVE.IPH.Infrastructure.*
    → EVE.IPH.Domain.Core        (interfaces only — no upward or lateral domain refs)

EVE.IPH.Domain.*
    → EVE.IPH.Domain.Core        (domains do not reference each other)

EVE.IPH.Domain.Core
    → (no project references)
```

If you find yourself wanting to reference one domain from another, introduce a shared abstraction in `Domain.Core` instead.

---

## Coding Standards

### General
- Use C# 12 language features where they improve clarity (primary constructors, collection expressions, etc.).
- Prefer `record` types for immutable data models; use `class` only when mutability is genuinely required.
- Use `readonly` wherever possible on fields and parameters.
- Avoid `var` when the type is not immediately obvious from the right-hand side.
- File-scoped namespaces (`namespace EVE.IPH.Domain.Core;`).
- One type per file; file name matches type name.

### Naming
- Types and public members: `PascalCase`
- Private fields: `_camelCase`
- Local variables and parameters: `camelCase`
- Interfaces: `IFoo`
- Async methods: suffix `Async` (e.g., `GetPricesAsync`)
- Constants: `PascalCase` (not `ALL_CAPS`)

### Error Handling
- Do **not** use exceptions for control flow. Use `Result<T>` from `Domain.Core` for operations that can legitimately fail.
- Exceptions are for truly exceptional/unexpected conditions (e.g., I/O failure, corrupt data).
- Never use bare `catch (Exception)` without logging and rethrowing or mapping to a `Result`.
- Do **not** swallow errors silently (no equivalent of VB's `On Error Resume Next`).

### Async
- All I/O must be `async`/`await`; never use `.Result` or `.Wait()` on a `Task`.
- Always pass and honour `CancellationToken` through the full call stack.
- Do not use `async void` except for top-level UI event handlers (Avalonia).

### Dependency Injection
- Register all services in the composition root (`EVE.IPH.UI.Avalonia` `App.axaml.cs`).
- Use constructor injection only; avoid property injection and service locator patterns.
- Prefer `IHttpClientFactory` for `HttpClient` instances.

### Testing
- Test framework: **xUnit**
- Mocking: **NSubstitute**
- Assertions: **FluentAssertions**
- Name tests: `MethodName_Scenario_ExpectedBehaviour` (e.g., `Calculate_WithMaxMeSkill_ReturnsReducedMaterials`).
- One test class per production class. Keep tests in the same namespace with `.Tests` appended to the project name.
- Use `[Theory]` with `[InlineData]` or `[MemberData]` for data-driven cases.
- Integration tests that hit the real SQLite file are placed in a separate `*.Integration.Tests` project.

### ESI / HTTP
- Use typed `HttpClient` clients registered via `IHttpClientFactory`.
- All ESI calls must use Polly for retry and rate-limit handling (exponential back-off on 5xx; honour `X-Esi-Error-Limit-Remain`).
- Store OAuth2 tokens encrypted; never log token values.

### Database
- Use `Microsoft.Data.Sqlite` with Dapper for query mapping.
- All queries are parameterised — never concatenate user input into SQL strings.
- Repositories are the only place SQL appears; form or service code must not contain SQL.
- Schema migrations are managed via versioned SQL scripts; no hand-edited schema changes.

### Logging
- Use `Microsoft.Extensions.Logging` abstractions (`ILogger<T>`); inject via constructor.
- Use Serilog as the concrete sink (file + optional console).
- Log at `Information` for significant state changes, `Warning` for recoverable issues, `Error` for unexpected failures. Avoid `Debug` spam in production paths.

---

## Pull Request Guidelines

- Every PR must target a specific phase item from the modernisation plan or address a bug.
- PRs that touch `src/` must include or update tests.
- PRs must not modify the legacy VB.NET files unless that is their explicit purpose (e.g., a hotfix on the legacy app).
- CI must be green before merging.

---

## Key Reference Documents

| Document | Location |
|---|---|
| Architecture Assessment | [`docs/architecture-assessment.md`](../docs/architecture-assessment.md) |
| Modernisation Plan | [`docs/modernisation-plan.md`](../docs/modernisation-plan.md) |
| Modernisation Solution | [`EVE-IPH-Modern.sln`](../EVE-IPH-Modern.sln) |
| Shared Build Properties | [`Directory.Build.props`](../Directory.Build.props) |
