# EVE Isk per Hour — Modernisation Plan

## 1. Goals

| Goal | Rationale |
|---|---|
| Cross-platform support (Windows, macOS, Linux) | EVE Online is played on all three platforms |
| Maintainable, testable codebase | Current codebase has zero tests and a 22,000-line god-object form |
| Clear separation between domain logic and UI | Enables independent development, testing, and UI replacement |
| Stay on .NET | Preserve existing VB.NET domain knowledge; migrate to C# for better ecosystem support |
| Avalonia UI as the new UI framework | The only mature, production-ready cross-platform XAML UI framework for .NET |
| Incremental delivery via the Strangler Fig pattern | Avoid a big-bang rewrite; keep the application usable throughout migration |

---

## 2. Target Architecture

The modernised application will be split into a set of domain library projects and a thin Avalonia UI host, all living in a single .NET solution.

```
EVE-IPH (solution)
├── src/
│   ├── EVE.IPH.Domain.Core/            # Shared types, interfaces, value objects
│   ├── EVE.IPH.Domain.Characters/      # Character, Corporation, ESI auth, Skills, Standings
│   ├── EVE.IPH.Domain.Market/          # Market data retrieval, caching, price models
│   ├── EVE.IPH.Domain.Manufacturing/   # Blueprint calculations, facility modelling, invention
│   ├── EVE.IPH.Domain.Reprocessing/    # Reprocessing/refining, mining belt flip analysis
│   ├── EVE.IPH.Domain.ShoppingList/    # Material aggregation, build-vs-buy decisions
│   ├── EVE.IPH.Domain.Industry/        # Industry jobs, research agents, datacores
│   ├── EVE.IPH.Domain.Assets/          # Character/corp assets
│   ├── EVE.IPH.Infrastructure.ESI/     # ESI HTTP client, OAuth2 PKCE, rate limiting
│   ├── EVE.IPH.Infrastructure.Data/    # SQLite data access (repositories, migrations)
│   ├── EVE.IPH.Infrastructure.Settings/# Settings persistence (JSON, per-platform paths)
│   └── EVE.IPH.UI.Avalonia/            # Avalonia UI host (ViewModels, Views, App entry point)
└── tests/
    ├── EVE.IPH.Domain.Manufacturing.Tests/
    ├── EVE.IPH.Domain.Market.Tests/
    ├── EVE.IPH.Domain.Reprocessing.Tests/
    └── EVE.IPH.Domain.ShoppingList.Tests/
```

### Dependency Rules

```
UI.Avalonia  →  Domain.*  +  Infrastructure.*
Infrastructure.*  →  Domain.Core  (no upward or lateral domain references)
Domain.*  →  Domain.Core  (domains do not reference each other)
Domain.Core  →  (no project references)
```

The domain libraries have no knowledge of the UI or infrastructure. This means domain logic can be unit tested in isolation without a running database, network, or UI thread.

---

## 3. Technology Choices

| Concern | Choice | Reason |
|---|---|---|
| Language | C# (.NET 8 LTS) | Better ecosystem, tooling, and language features than VB.NET; .NET 8 is cross-platform and supported until Nov 2026 |
| UI | Avalonia UI (v11+) | Cross-platform XAML; MVVM-compatible; active community; closest in feel to WPF |
| UI pattern | MVVM (with CommunityToolkit.Mvvm) | Clean separation; Avalonia has first-class MVVM support |
| Database | SQLite via Microsoft.Data.Sqlite + Dapper | Lightweight, familiar, cross-platform; Dapper gives type-safe queries without full ORM overhead |
| HTTP | System.Net.Http.HttpClient + Polly | Proper async, retry policies, circuit breaking, rate limiting |
| DI | Microsoft.Extensions.DependencyInjection | Standard .NET DI; replaces all global singletons |
| Settings | System.Text.Json to platform AppData | JSON settings with versioned migration; replaces XML serialisation |
| Logging | Microsoft.Extensions.Logging + Serilog | Structured logging to file and optionally to console |
| Linear programming | Google OR-Tools (NuGet, x64/arm64 support) | Replaces LpSolveDotNet which is x86 native-only |
| Testing | xUnit + NSubstitute + FluentAssertions | Standard .NET test stack |
| Build | `dotnet` CLI + GitHub Actions | Cross-platform CI; replaces MSBuild-only solution |

---

## 4. Strangler Fig Migration Strategy

The Strangler Fig pattern replaces a legacy system incrementally by routing specific functional slices to new implementations while the old system continues to run. Each phase extracts one domain slice, builds the new library with tests, and wraps the legacy code to call the new library. The legacy form code is removed once the new UI surface is live for that slice.

Migration is ordered from the most foundational (data access, shared models) to the most complex UI-heavy features (manufacturing tab). This ensures every later phase builds on verified, tested infrastructure.

---

## 5. Phase Plan

### Phase 0 — Groundwork (Pre-migration)
**Objective:** Set up the solution structure and build pipeline without touching any application behaviour.

- Current repo status: Phase 0 is complete for the current milestone. The modern .NET 8 solution, project structure, shared build props, `.editorconfig` baseline, and GitHub Actions build/test workflow are now in place.

- [x] Create a new .NET 8 solution alongside the existing `.vbproj` project
- [x] Add all library and UI projects to the solution with correct project references
- [x] Set up GitHub Actions CI: build and test the modern solution on GitHub Actions
- [x] Establish coding standards with `.editorconfig` and the shared modern project conventions
- [x] Add `Directory.Build.props` with shared version, nullable enable, and treat-warnings-as-errors settings

**Output:** A compiling modern solution skeleton is checked in alongside the legacy app with automated GitHub Actions build and test coverage.

---

### Phase 1 — Core & Shared Types
**Objective:** Define the shared vocabulary of the domain with no logic yet.

Projects: `EVE.IPH.Domain.Core`

- Current repo status: Phase 1 is complete for the current milestone. `Domain.Core` contains identifier value objects, `Result<T>`, `Maybe<T>`, `Error`, core enumerations, and the shared repository and service interfaces now consumed throughout the modern solution.

- [x] Define shared value types such as `TypeId`, `ItemId`, `RegionId`, `SystemId`, `CharacterId`, and `CorporationId`
- [x] Define shared result types such as `Result<T>` and `Maybe<T>` for error handling without exceptions
- [x] Define the shared interfaces used by infrastructure and domain services, including market, character, blueprint, industry, shopping-list, and settings contracts
- [x] Define shared enumerations and supporting records used by the extracted domains
- [x] Write focused unit tests for identifiers and core result types

**Output:** `EVE.IPH.Domain.Core` is fully tested and published as a project reference.

---

### Phase 2 — Data Access Infrastructure
**Objective:** Replace the global `DBConnection` / raw SQL pattern with a structured repository layer.

Projects: `EVE.IPH.Infrastructure.Data`

- Current repo status: Phase 2 is complete for the current milestone. The modern data layer includes SQLite connection and migration support plus repository implementations for SDE data and application data such as characters, skills, standings, research agents, owned blueprints, market cache, industry jobs, assets, and shopping lists.

- [x] Implement `IDbConnectionFactory` using `Microsoft.Data.Sqlite`
- [x] Set up Dapper for typed query mapping in the repository layer
- [x] Implement read-oriented SDE repositories for blueprints, items, groups, and regions
- [x] Implement application-data repositories for characters, owned blueprints, market cache, shopping lists, industry jobs, assets, and related records
- [x] Write integration tests against SQLite-backed repository flows
- [x] Keep the modern schema and persistence model aligned with the shared SQLite-based application data format used during migration

**Output:** Database access is available through typed repository interfaces and validated repository implementations.

---

### Phase 3 — Settings Infrastructure
**Objective:** Replace XML settings serialisation with JSON-based, versioned, platform-aware settings.

Projects: `EVE.IPH.Infrastructure.Settings`

- Current repo status: Phase 3 is complete for the current milestone. The repo now contains JSON-backed settings storage, platform-aware storage-path handling, XML migration support, and focused settings tests.

- [x] Define the modern settings models used by the extracted services
- [x] Implement `ISettingsStore` with JSON read/write using `System.Text.Json`
- [x] Support per-platform storage paths for Windows, macOS, and Linux
- [x] Implement a one-off migration path to convert existing XML settings files to JSON on first run
- [x] Write focused tests for serialisation round-trips and migration

**Output:** Settings can be loaded and saved cross-platform; existing user settings are migrated automatically.

---

### Phase 4 — ESI Infrastructure
**Objective:** Replace the hand-rolled `ESI` class with a properly async, testable HTTP client with rate limiting and retry.

Projects: `EVE.IPH.Infrastructure.ESI`

- Current repo status: Phase 4 is complete for the current milestone. The repo now contains a typed ESI client, PKCE authorization flow, token exchange and refresh handling, persisted token storage, retry and error-limit handling, and the typed character- and market-facing adapters required by the extracted domains.

- [x] Implement the typed ESI HTTP client using `HttpClient` and dependency injection
- [x] Implement OAuth2 PKCE flow with authorisation redirect, local callback listener, token exchange, and token refresh
- [x] Implement persisted token storage for the modern ESI flow
- [x] Add retry and ESI error-limit handling for transient failures
- [x] Implement the typed ESI adapters required by the extracted character and market workflows
- [x] Write focused tests covering retry behaviour, token refresh, and ESI request handling

**Output:** All ESI calls are async, have retry/rate-limit handling, and are accessible via a typed interface.

---

### Phase 5 — Character Domain
**Objective:** Extract the character and authentication domain.

Projects: `EVE.IPH.Domain.Characters`

- Current repo status: Phase 5 is complete for the current milestone. `Domain.Characters` now covers repository-backed character refresh/load, skill lookup and overrides, standings, market-tax calculations, research-agent orchestration, and datacore valuation shaping.

- [x] Define the character-domain models, including character, corporation, skills, standings, and research-agent records
- [x] Implement repository-backed character services that load from ESI, persist, and retrieve snapshots
- [x] Implement skill services that load skills, apply overrides, and query effective levels
- [x] Implement standings and tax services that compute derived NPC tax values
- [x] Write focused unit tests covering skill lookups, standings, tax calculations, and research-agent flows

**Output:** Character, skills, standings can be loaded, queried, and tested without a running UI.

---

### Phase 6 — Market Domain
**Objective:** Extract market data retrieval and caching into a testable library.

Projects: `EVE.IPH.Domain.Market`

- Current repo status: Phase 6 is complete for the current milestone. `Domain.Market` now contains provider-backed market-price lookup with cache-aware orchestration, settings-driven provider selection, and concrete Tranquility, EVEMarketer, and Fuzzworks sources.

- [x] Define the market models and provider abstractions needed by the modern price-lookup flow
- [x] Implement provider-backed market price sources for Tranquility, EVEMarketer, and Fuzzworks
- [x] Implement cache-aware orchestration that respects the modern cache rules
- [x] Implement the market service that resolves providers and performs batch price lookups
- [x] Write focused unit tests for caching logic, provider selection, and price aggregation

**Output:** Market price retrieval is decoupled from the UI and ESI client; any provider can be swapped or mocked.

---

### Phase 7 — Manufacturing Domain
**Objective:** Extract the core blueprint profitability calculation engine.

Projects: `EVE.IPH.Domain.Manufacturing`

Current repo status: the deterministic Phase 7 manufacturing milestone is complete. The repo now contains extracted material/time, profitability, invention planning, activity, cost rollup, timeline, component scheduling, prerequisite, facility usage, usage allocation, sale-adjustment, build-vs-buy, and composed manufacturing-analysis seams with focused executable tests.

- [x] Extract the deterministic manufacturing calculation seams needed for the current milestone
- [x] Add focused unit tests for the extracted manufacturing services using fixed input fixtures
- [x] Compose the extracted seams into the current `ManufacturingAnalysisService` workflow
- [x] Validate representative legacy-compatible manufacturing scenarios through executable tests

This is the most critical phase. The formulas in `Blueprint.vb` and `frmMain.vb` must be extracted faithfully and verified against known-good results from the existing application.

**Output:** The deterministic manufacturing calculations targeted for this milestone are covered by unit tests and produce legacy-compatible results.

---

### Phase 8 — Reprocessing Domain
**Objective:** Extract reprocessing and mining analysis logic.

Projects: `EVE.IPH.Domain.Reprocessing`

Current repo status: the deterministic Phase 8 reprocessing milestone is complete. The repo now contains extracted reprocessing yield/output, ore-conversion optimization over already-resolved candidate yields and objective values, and belt-flip profitability over already-resolved belt lines, volumes, and market values, all with focused executable tests.

- [x] Extract the deterministic reprocessing calculation seams needed for the current milestone
- [x] Add focused unit tests for the extracted reprocessing services using fixed input fixtures
- [x] Compose the extracted seams into the current reprocessing and belt-flip analysis flows
- [x] Validate representative legacy-compatible reprocessing scenarios through executable tests

**Output:** The deterministic reprocessing calculations targeted for this milestone are covered by unit tests and separated from the UI.

---

### Phase 9 — Shopping List Domain
**Objective:** Extract the shopping list and material aggregation logic.

Projects: `EVE.IPH.Domain.ShoppingList`

- Current repo status: Phase 9 is complete for the current milestone. The repo now contains shopping-list models, repository-backed load/save orchestration, material aggregation, duplicate merging, build-vs-buy projections, invention/copy/final-item list views, on-hand subtraction, and focused executable tests.

- [x] Define `ShoppingListItem`, `MaterialList`, `AggregatedShoppingList` models
- [x] Implement `IShoppingListService`: add items, aggregate materials, apply build-vs-buy decisions, compute total cost
- [x] Implement serialisation of a shopping list to/from the SQLite repository
- [x] Write unit tests for material aggregation, deduplication, and cost rollup

**Output:** Shopping list logic is isolated and testable.

---

### Phase 10 — Assets & Industry Jobs Domain
**Objective:** Extract character assets and industry job tracking.

Projects: `EVE.IPH.Domain.Assets`, extended `EVE.IPH.Domain.Industry`

- Current repo status: Phase 10 is complete for the current milestone. `Domain.Assets` now contains asset snapshot hydration, asset display formatting, tree projection, material-ancestor filtering, and non-UI asset-view filtering rules with focused tests. `Domain.Industry` now contains industry-job state classification, active-job summarization, character-scoped and corporation-scoped refresh/load orchestration, and presentation-row shaping with focused tests. The remaining datacore view logic has been lifted into the character domain as research-agent datacore valuation.

- [x] Extract the first deterministic industry-job state and summarization seams
- [x] Extract the first deterministic asset display/tree/ancestor seams
- [x] Add focused tests for those initial assets and industry services
- [x] Introduce corporation-scoped industry-job refresh/orchestration that preserves legacy installer filtering and grouped persistence semantics
- [x] Introduce repository-backed asset snapshot hydration and remaining non-UI asset-view filtering/grouping rules
- [x] Fold remaining datacore/agent and industry-view shaping logic into the modern domain layer before starting meaningful Avalonia work

**Output:** Assets, industry jobs, and datacore valuation are testable domain services with repository-backed workflows sufficient to support the first real Avalonia screens.

---

### Phase 11 — Avalonia UI Host
**Objective:** Build the new cross-platform UI shell using Avalonia with MVVM.

Projects: `EVE.IPH.UI.Avalonia`

- Current repo status: Phase 11 is now underway. `EVE.IPH.UI.Avalonia` is a real Avalonia desktop host with desktop lifetime bootstrap, DI composition root, a shell window, reusable modal dialog infrastructure, first-run/update/import dialog seams, JSONL SDE bootstrap/version surfacing, and initial screens for assets, industry jobs, research agents/datacores, settings, and character/account management. The shell now supports multi-character onboarding, a generated local `All Skills V` placeholder, character-keyed token persistence, explicit corporation connections for corporation-scoped asset/job data, token-health surfacing in character management, and mixed-owner asset filtering across characters and corporations. A dedicated `EVE.IPH.UI.Avalonia.Tests` project now covers the first view-model and shell-seam tests for import, restart-required state, filtering, summary projection, character-management workflows, and async loading/error behavior.

- [x] Bootstrap the Avalonia host itself: add the `App`, `Program`, desktop lifetime wiring, theme/resources, and first main window so the project is a real runnable UI application rather than only a referenced project shell
- [x] Configure the DI container wiring the existing infrastructure implementations and Phase 5-10 domain services into the Avalonia composition root
- [x] Implement the application shell: main window, navigation structure, and simple status/loading surfaces that can host multiple feature views
- [x] Implement the first read-only MVVM slices (using `CommunityToolkit.Mvvm`) for the already extracted Phase 10 surfaces:
  - Assets viewer
  - Industry jobs viewer
  - Datacores / research agents tab
- [x] Add explicit legacy database import flow, restart prompt, and reusable modal shell-dialog infrastructure instead of implicitly adopting old database paths at startup
- [x] Add focused view-model tests and seam validation for navigation, loading, filtering, empty states, and error handling in those first screens
- [ ] Before expanding the UI further, harden the application seams the shell still depends on:
  - Persist and validate corporation capabilities explicitly, not just scopes, so the modern host can enforce the legacy corporation model (`Director` for assets/blueprints, `Factory Manager` for jobs)
  - Add the missing corporation-blueprint path end to end: ESI adapter, data-source/repository layer, persistence, refresh orchestration, and read models that can be consumed by manufacturing and blueprint-management UI
  - Define a stable screen-facing application-service layer so future tabs bind to query/command services instead of accumulating repository- and workflow-specific wiring inside view-models
  - Finish the manufacturing-input seams that are still UI-blocking, especially structure/facility persistence and the remaining editable blueprint ownership/workflow state needed before the manufacturing and Upwell structure tabs are rebuilt
  - Add integration coverage for onboarding and delete cascades across characters, corporation connections, mixed-owner assets, token state, and the future corporation-blueprint flow
- [ ] After the first read-only screens are stable, migrate the next feature areas one tab at a time:
  - Blueprint management and corporation blueprint views
  - Manufacturing tab
  - Upwell structure fitting / facility management
  - Market prices / update tab
  - Shopping list
  - Mining / reprocessing tab
  - Settings refinements beyond the current shell status surface
  - Character / corporation management refinements beyond the current connect/refresh/default/remove workflow
- [ ] Reassess whether a splash screen or explicit loading workflow is still justified once the first shell and read-only tabs are running
- [ ] Reassess whether the legacy self-update flow should be reused, replaced, or deferred once packaging and release distribution are defined

**Recommended next continuation order:**

1. Treat the next tranche as foundation hardening rather than new UI breadth: finish explicit corporation capability persistence and role validation so corporation features match legacy intent before more tabs consume them.
2. Build the missing corporation-blueprint slice end to end and unify personal/corporation blueprint ownership into a single modern model the shell can query.
3. Extract the remaining manufacturing-input seams the next tabs depend on, especially structure/facility persistence and editable blueprint ownership workflows, so the manufacturing and Upwell screens are not forced to recreate legacy form behavior in view-models.
4. Consolidate screen-facing application services around query/command boundaries and add integration coverage for character/corporation onboarding, refresh, delete, and mixed-owner projections.
5. Only after those seams are stable, continue tab migration in this order: blueprint management, manufacturing, Upwell structures/facilities, market/update, shopping list, then mining/reprocessing and the remaining shell refinements.

**Output:** The application has a real Avalonia shell backed by the modern service layer, with the first read-only screens proving that the extracted domains can power the new UI without legacy VB code.

---

### Phase 12 — Decommission Legacy Application
**Objective:** Remove the legacy VB.NET WinForms project once the Avalonia app reaches full feature parity.

- [ ] Confirm all legacy features are implemented and tested in the new UI
- [ ] Conduct a regression pass comparing output of domain library calculations against the legacy application
- [ ] Archive the legacy `EVE Isk per Hour.vbproj` and associated files (or move to a `legacy/` folder for reference)
- [ ] Update CI to remove the legacy build
- [ ] Update distribution / release pipeline for the new cross-platform build targets

---

## 6. Strangler Boundary Management

During the migration (Phases 1–11), both the legacy WinForms app and the new libraries coexist in the same solution. The strangler boundary is managed as follows:

1. **New libraries are written in C#** targeting `net8.0`. The legacy VB.NET project targets `net461` and cannot reference them directly.
2. **The legacy app is not modified** to call into new libraries — it continues to run as before during development.
3. **Testing validates equivalence**: domain library unit tests use reference input/output pairs captured from the running legacy app. Once a domain's tests pass, the legacy implementation for that domain is considered superseded.
4. **The Avalonia UI calls only the new domain libraries** — no references to VB.NET files.
5. **The SQLite database schema is the bridge**: both apps read from the same SQLite file format. The data migration (Phase 3) ensures settings files can also coexist.

---

## 7. Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Formula discrepancies between legacy and new implementations | High | High | Capture known-good calculation outputs from legacy app as test fixtures before beginning Phase 7 |
| ESI API changes invalidating migration work | Medium | Medium | ESI client is isolated in its own library; only one place to update |
| LpSolveDotNet replacement breaks build-vs-buy linear programming | Medium | Medium | Google OR-Tools is API-compatible for simple LP; validate with unit tests |
| Avalonia rendering differences from WinForms on some platforms | Medium | Low | Avalonia has mature cross-platform support; XAML styling can be iterated independently |
| Loss of existing user settings on migration | Low | High | Phase 3 implements an explicit migration from XML to JSON; tested before release |
| Scope creep (adding new features during migration) | High | Medium | Enforce a strict "feature freeze on new features until Phase 11 complete" policy |
| VB.NET domain knowledge not transferring to C# during rewrite | Medium | High | Work through the legacy code domain by domain; do not assume — verify with tests |

---

## 8. Success Criteria

- All calculation results from the new domain libraries match the legacy application within floating-point tolerance for a set of reference scenarios.
- The Avalonia application starts and is fully usable on Windows 10+, macOS 13+, and Ubuntu 22.04+.
- All domain libraries have >80% unit test coverage.
- No global mutable state exists in the new codebase.
- CI builds and tests pass on all three platforms.
- Existing user settings (SQLite database, settings files) are successfully migrated on first run of the new application.
