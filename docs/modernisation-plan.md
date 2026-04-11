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

- [ ] Create a new .NET 8 solution alongside the existing `.vbproj` project
- [ ] Add all library and UI projects to the solution with correct project references
- [ ] Set up GitHub Actions CI: build all projects, run tests, produce release artefacts for Windows, macOS, Linux
- [ ] Establish coding standards: EditorConfig, StyleCop/Roslyn analyser baseline
- [ ] Add `Directory.Build.props` with shared version, nullable enable, and treat-warnings-as-errors settings

**Output:** A compiling (but mostly empty) solution skeleton checked in alongside the legacy app.

---

### Phase 1 — Core & Shared Types
**Objective:** Define the shared vocabulary of the domain with no logic yet.

Projects: `EVE.IPH.Domain.Core`

- [ ] Define shared value types: `TypeId`, `ItemId`, `RegionId`, `SystemId`, `CharacterId`, `CorporationId`
- [ ] Define shared result types: `Result<T>`, `Maybe<T>` for error handling without exceptions
- [ ] Define interfaces that infrastructure will implement: `IMarketPriceSource`, `ICharacterRepository`, `IBlueprintRepository`, `ISettingsStore`
- [ ] Define shared enumerations: `ActivityType` (Manufacturing, Copying, Invention, Reaction), `TechLevel`, `ScanType`
- [ ] Write unit tests for all value types

**Output:** `EVE.IPH.Domain.Core` is fully tested and published as a project reference.

---

### Phase 2 — Data Access Infrastructure
**Objective:** Replace the global `DBConnection` / raw SQL pattern with a structured repository layer.

Projects: `EVE.IPH.Infrastructure.Data`

- [ ] Implement `IDbConnectionFactory` using `Microsoft.Data.Sqlite`
- [ ] Set up Dapper for type-safe query mapping
- [ ] Implement read-only SDE repositories: `IBlueprintRepository`, `IItemRepository`, `IGroupRepository`, `IRegionRepository`
- [ ] Implement write repositories: `ICharacterRepository`, `IOwnedBlueprintRepository`, `IMarketCacheRepository`
- [ ] Write integration tests against an in-memory SQLite database seeded with a minimal SDE snapshot
- [ ] Ensure the legacy app can read from the same SQLite file — schema must be backward compatible at this stage

**Output:** All database access is available via typed repository interfaces; the global `EVEDB` singleton can start being replaced call by call.

---

### Phase 3 — Settings Infrastructure
**Objective:** Replace XML settings serialisation with JSON-based, versioned, platform-aware settings.

Projects: `EVE.IPH.Infrastructure.Settings`

- [ ] Define settings models as plain C# records (one per settings group matching the existing VB classes)
- [ ] Implement `ISettingsStore` with JSON read/write using `System.Text.Json`
- [ ] Support per-platform storage paths (Windows AppData, macOS Library/Application Support, Linux ~/.config)
- [ ] Implement a one-off migration path to convert existing XML settings files to JSON on first run
- [ ] Write unit tests for serialisation round-trips and migration

**Output:** Settings can be loaded and saved cross-platform; existing user settings are migrated automatically.

---

### Phase 4 — ESI Infrastructure
**Objective:** Replace the hand-rolled `ESI` class with a properly async, testable HTTP client with rate limiting and retry.

Projects: `EVE.IPH.Infrastructure.ESI`

- [ ] Implement `IESIClient` with `HttpClient` (registered as a typed client via `IHttpClientFactory`)
- [ ] Implement OAuth2 PKCE flow: authorisation redirect, local callback listener, token exchange, token refresh
- [ ] Implement token storage (encrypted on platform keychain where available)
- [ ] Add Polly retry policy: exponential back-off on 5xx, respect ESI `X-Esi-Error-Limit-Remain` header
- [ ] Implement all ESI scopes used by the application (see `ESI.vb` scope constants) as typed method calls
- [ ] Write unit tests using mocked `HttpMessageHandler` to verify retry behaviour, token refresh, and scope handling

**Output:** All ESI calls are async, have retry/rate-limit handling, and are accessible via a typed interface.

---

### Phase 5 — Character Domain
**Objective:** Extract the character and authentication domain.

Projects: `EVE.IPH.Domain.Characters`

- [ ] Define `Character`, `Corporation`, `Skill`, `SkillList`, `NpcStanding`, `ResearchAgent` as immutable C# records
- [ ] Implement `ICharacterService`: load character from ESI, persist to repository, retrieve from repository
- [ ] Implement `ISkillService`: load skills from ESI, apply overrides, query effective level
- [ ] Implement `IStandingsService`: load standings, compute effective NPC tax rates
- [ ] Write unit tests covering skill level lookups and tax rate calculations

**Output:** Character, skills, standings can be loaded, queried, and tested without a running UI.

---

### Phase 6 — Market Domain
**Objective:** Extract market data retrieval and caching into a testable library.

Projects: `EVE.IPH.Domain.Market`

- [ ] Define `MarketPrice`, `PriceHistory`, `MarketOrder` models
- [ ] Implement `IMarketPriceProvider` with three implementations: ESI, EVEMarketer, Fuzzworks
- [ ] Implement a caching decorator over `IMarketPriceProvider` that enforces cache expiry rules from `CacheBox.vb`
- [ ] Implement `IMarketService` that orchestrates provider selection and batch price lookups
- [ ] Write unit tests for caching logic, provider selection, and price aggregation

**Output:** Market price retrieval is decoupled from the UI and ESI client; any provider can be swapped or mocked.

---

### Phase 7 — Manufacturing Domain
**Objective:** Extract the core blueprint profitability calculation engine.

Projects: `EVE.IPH.Domain.Manufacturing`

- [ ] Define `Blueprint`, `ManufacturingRun`, `FacilityBonus`, `IndustryFacility`, `Decryptor` models
- [ ] Implement `IBlueprintCalculator`: given a blueprint, facility, character skills, market prices → return `ManufacturingResult` with profit, cost, ISK/hour, time
- [ ] Implement `IInventionCalculator`: given a T1 blueprint and decryptor → return `InventionResult` with probability, resulting T2 blueprint stats
- [ ] Implement `IFacilityBonusCalculator`: compute rig, structure, and implant bonuses for ME/TE/cost
- [ ] Implement `IBuildBuyDecider`: given a bill of materials and market prices → return buy vs. build decision for each component
- [ ] Write comprehensive unit tests for all calculations using fixed test data (SDE values and prices)

This is the most critical phase. The formulas in `Blueprint.vb` and `frmMain.vb` must be extracted faithfully and verified against known-good results from the existing application.

**Output:** All manufacturing calculations are covered by unit tests and produce identical results to the legacy code.

---

### Phase 8 — Reprocessing Domain
**Objective:** Extract reprocessing and mining analysis logic.

Projects: `EVE.IPH.Domain.Reprocessing`

- [ ] Define `ReprocessingResult`, `OreYield`, `MiningBeltFlip` models
- [ ] Implement `IReprocessingCalculator`: given ore type, quantity, skills, facility efficiency → return refined materials and taxes
- [ ] Implement `IOreConversionService`: convert between ore groups (compressed, raw, moon ore variants)
- [ ] Implement `IBeltFlipCalculator`: given a belt composition and market prices → return profitability of reprocessing vs. raw sale
- [ ] Write unit tests for yield calculations and belt flip comparisons

**Output:** Reprocessing logic is tested and separated from the UI.

---

### Phase 9 — Shopping List Domain
**Objective:** Extract the shopping list and material aggregation logic.

Projects: `EVE.IPH.Domain.ShoppingList`

- [ ] Define `ShoppingListItem`, `MaterialList`, `AggregatedShoppingList` models
- [ ] Implement `IShoppingListService`: add items, aggregate materials, apply build-vs-buy decisions, compute total cost
- [ ] Implement serialisation of a shopping list to/from the SQLite repository
- [ ] Write unit tests for material aggregation, deduplication, and cost rollup

**Output:** Shopping list logic is isolated and testable.

---

### Phase 10 — Assets & Industry Jobs Domain
**Objective:** Extract character assets and industry job tracking.

Projects: `EVE.IPH.Domain.Assets`, extended `EVE.IPH.Domain.Industry`

- [ ] Define `Asset`, `AssetLocation` models; implement `IAssetService`
- [ ] Define `IndustryJob`, `ResearchAgent`, `DatacoreAgent` models; implement `IIndustryJobService`
- [ ] Write unit tests for asset grouping and industry job status queries

**Output:** Assets and jobs are testable domain services.

---

### Phase 11 — Avalonia UI Host
**Objective:** Build the new cross-platform UI shell using Avalonia with MVVM.

Projects: `EVE.IPH.UI.Avalonia`

- [ ] Set up Avalonia 11 application project targeting `net8.0`
- [ ] Configure the DI container wiring all domain services and infrastructure implementations
- [ ] Implement the application shell: main window, tab navigation, status bar, progress indicators
- [ ] Implement ViewModels (using `CommunityToolkit.Mvvm`) and Views (AXAML) for each functional area, migrating one tab at a time:
  - Manufacturing tab (highest priority — core feature)
  - Market prices / update tab
  - Mining / reprocessing tab
  - Datacores / research agents tab
  - Industry jobs viewer
  - Shopping list
  - Assets viewer
  - Settings
  - Character / account management
  - Blueprint management
  - Upwell structure fitting
- [ ] Implement the splash screen and loading sequence
- [ ] Implement the self-update check (can re-use `ProgramUpdater` logic initially)

**Output:** The application runs natively on Windows, macOS, and Linux with full feature parity to the legacy WinForms app.

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
