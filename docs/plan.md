# EVE-IPH Modernisation — Next Milestone Plan

## Current State (as of April 2026)

The repository has moved beyond the initial scaffold. The modern solution exists, builds, and includes implemented C# code plus passing tests for the shared foundation layers.

**Completed or substantially complete:**

- **Phase 0 — Groundwork:** `EVE-IPH-Modern.slnx`, all planned `src/` and `tests/` projects, `Directory.Build.props`, and `.editorconfig` are present.
- **Phase 1 — Domain.Core:** identifier value objects, `Result<T>`, `Maybe<T>`, `Error`, core enumerations, and the shared repository/service contracts now used by characters, market, settings, and data infrastructure are implemented.
- **Phase 2 — Infrastructure.Data:** SQLite connection factory, migration runner, and repository implementations for SDE plus app data access are implemented, including market cache, characters, skills, standings, research agents, owned blueprints, and shopping lists.
- **Phase 3 — Infrastructure.Settings:** JSON settings store, platform-specific storage path handling, XML-to-JSON migration support, and settings-backed market source preference resolution are implemented.
- **Phase 4 — Infrastructure.ESI:** typed ESI HTTP client, retry/error-limit handling, PKCE authorization request generation, token exchange/refresh, interactive localhost callback flow, file-based token persistence, and typed character/market-facing adapters are implemented.
- **Phase 5 — Domain.Characters:** skill lookup, standings, character-derived market tax services, repository-backed character orchestration, SQLite-backed skills/standings/research-agent persistence, and ESI-backed character plus research-agent data sources are now implemented.
- **Phase 6 — Domain.Market:** provider-backed market price lookup is now implemented with cache-aware orchestration, settings-driven provider selection, and concrete Tranquility, EVEMarketer, and Fuzzworks sources.
- **Phase 9 — Domain.ShoppingList:** material aggregation, legacy-compatible duplicate merging, repository-backed load/save orchestration, build-vs-buy projections, invention/copy/final-item list views, and on-hand subtraction are now implemented for the current milestone.

**Verified state:**

- `dotnet test .\\EVE-IPH-Modern.slnx --configuration Release` passes.
- The current test suite contains **204 passing tests**, including focused `Domain.Manufacturing`, `Domain.Reprocessing`, `Domain.ShoppingList`, `Domain.Characters`, `Infrastructure.ESI`, `Infrastructure.Data`, `Infrastructure.Settings`, and `Domain.Market` coverage.

**Started but not yet complete:**

- **Phase 7 — Domain.Manufacturing:** the first deterministic manufacturing calculator slice is implemented, but broader blueprint profitability, invention, and build-vs-buy orchestration are still ahead.
- **Phase 8 — Domain.Reprocessing:** the first deterministic reprocessing yield/output calculator slice is implemented, but ore-conversion and belt-flip workflows are still ahead.

**Not yet implemented in a meaningful way:**

- `EVE.IPH.Domain.Manufacturing`
- `EVE.IPH.Domain.Reprocessing`
- `EVE.IPH.Domain.Assets`
- `EVE.IPH.Domain.Industry`
- `EVE.IPH.UI.Avalonia`

`EVE.IPH.Domain.Assets`, `EVE.IPH.Domain.Industry`, and `EVE.IPH.UI.Avalonia` still exist mainly as project shells. `EVE.IPH.Domain.Manufacturing`, `EVE.IPH.Domain.Reprocessing`, and `EVE.IPH.Domain.ShoppingList` now have focused executable coverage and no longer belong in the shell-only category.

---

## Validation Summary for Phases 1–6

The current repository state matches the intended Phase 1–6 extraction goals closely enough to treat those phases as complete for the current milestone.

- **Phase 1 — Domain.Core validated:** value objects, `Result<T>`, `Maybe<T>`, `Error`, and shared contracts are present in `EVE.IPH.Domain.Core`, with executable coverage in `EVE.IPH.Domain.Core.Tests`.
- **Phase 2 — Infrastructure.Data validated:** `SqliteConnectionFactory`, `SqliteMigrationRunner`, and repository implementations for blueprints, items, regions, characters, skills, standings, research agents, market cache, owned blueprints, and shopping lists are present and covered by integration tests.
- **Phase 3 — Infrastructure.Settings validated:** `JsonSettingsStore`, platform storage path handling, XML migration, multiple settings models, and the market source preference provider are implemented and covered by focused settings tests.
- **Phase 4 — Infrastructure.ESI validated:** typed ESI transport, bearer-token handling, retry/rate-limit handling, PKCE login flow, local callback listener, token storage/provider logic, and typed ESI data mappings are implemented and covered by executable tests.
- **Phase 5 — Domain.Characters validated:** skills, standings, market-tax calculations, character snapshot refresh, and research-agent orchestration are implemented with focused domain and infrastructure coverage.
- **Phase 6 — Domain.Market validated:** cache hit/miss/expiry behaviour, provider resolution, settings-driven source selection, and concrete Tranquility, EVEMarketer, and Fuzzworks mapping/aggregation are implemented and covered by tests.

**Known gaps relative to the original Phase 0 checklist:**

- No GitHub Actions workflow is present yet.
- The Avalonia UI host project exists but does not contain application code.

---

## Next Milestone: Manufacturing Calculation Extraction (Phase 7)

**Goal:** Build out `Domain.Manufacturing` on top of the completed character and market foundations so blueprint cost, material requirement, and build-vs-buy workflows can move out of the legacy app.

**Why this is next:** the manufacturing formulas are the highest-value remaining legacy logic, and the prerequisite seams now exist: blueprint/item repositories live in Phase 2, character skills and standings live in Phase 5, and provider-backed market prices live in Phase 6.

**Initial extraction target:** start with the pure calculation path that turns one blueprint, one facility context, one character snapshot, and one market snapshot into a deterministic manufacturing result. Keep the first slice UI-free and persistence-light.

**Primary legacy files to study first:** `Blueprint.vb`, `ManufacturingFacility.vb`, manufacturing-heavy sections of `frmMain.vb`, `frmBlueprintManagement.vb`, `frmBlueprintList.vb`.

**Definition of ready for Phase 7:** capture a small set of known-good legacy manufacturing scenarios and promote them into repeatable tests before copying formulas.

---

### Phase 5 — `EVE.IPH.Domain.Characters` (character, skills, standings)

Once ESI infrastructure exists, the first real domain service layer can be extracted.

**Current progress:** this phase is now complete for the initial modernization milestone. The modern character slice covers profile refresh, skills, standings, market-tax calculations, and current research agents with repository-backed persistence and focused tests.

#### 5.1 Models

- `Character`
- `Corporation`
- `Skill`
- `SkillList`
- `NpcStanding`
- `ResearchAgent`

#### 5.2 Services

- `ICharacterService`
- `ISkillService`
- `IStandingsService`

#### 5.3 Behaviour to Implement

- Load character data from ESI and persist it through repositories
- Apply skill overrides and expose effective skill lookups
- Compute standings-derived tax values and related derived values used by the legacy app

#### 5.4 Tests

- Skill level lookup coverage
- Override behaviour coverage
- Standing and tax-rate calculation coverage

> **Reference files from legacy app:** `Character.vb`, `Corporation.vb`, `EVESkillList.vb`, `EVENPCStandings.vb`, `EVEResearchAgents.vb`

---

### Phase 6 — `EVE.IPH.Domain.Market` (market providers and caching)

This phase establishes the shared price lookup layer needed by manufacturing, reprocessing, and shopping list work.

**Current progress:** this phase is now complete for the current modernization milestone. The modern market slice includes a cache-aware market service, explicit provider resolution, settings-driven provider selection, and concrete Tranquility, EVEMarketer, and Fuzzworks adapters with executable tests.

#### 6.1 Models

- `MarketPrice`

#### 6.2 Services

- `IMarketPriceSource` implementations for Tranquility, EVEMarketer, and Fuzzworks
- Caching decorator using `IMarketCacheRepository`
- `IMarketService` to orchestrate provider selection and batch lookups

#### 6.3 Implemented Behaviour

- Batch price retrieval by type ID
- Cache expiry rules migrated from `CacheBox.vb`
- Provider selection driven by settings
- Tranquility order/history aggregation into the shared `MarketPrice` model

#### 6.4 Tests

- Cache hit/miss and expiry tests
- Provider selection tests
- Provider payload mapping tests
- Price aggregation tests

> **Reference files from legacy app:** `CacheBox.vb`, `EVEMarketer.vb`, `FuzzworksMarket.vb`, market-related code in `ESI.vb`

---

## Next Phase Details

### Phase 7 — `EVE.IPH.Domain.Manufacturing`

**Objective:** extract blueprint profitability, invention, facility bonuses, and build-vs-buy calculations into deterministic services.

**Planned first models/services:**

- `Blueprint`, `ManufacturingRun`, `IndustryFacility`, `FacilityBonus`, `Decryptor`, `ManufacturingResult`
- `IBlueprintCalculator`
- `IFacilityBonusCalculator`
- `IInventionCalculator`
- `IBuildBuyDecider`

**Dependencies already available:**

- SDE blueprint/item repositories from Phase 2
- Character skills and tax inputs from Phase 5
- Provider-backed market prices from Phase 6

**First testable slice:**

- material quantity calculation for a single manufacturing run
- time and facility bonus application
- profit / ISK-per-hour result for fixed input fixtures

### Phase 8 — `EVE.IPH.Domain.Reprocessing`

**Objective:** extract ore conversion, refining yield, and belt-flip profitability into a standalone domain library.

**Planned first models/services:**

- `ReprocessingResult`, `OreYield`, `MiningBeltFlip`
- `IReprocessingCalculator`
- `IOreConversionService`
- `IBeltFlipCalculator`

**Primary legacy files:** `ReprocessingPlant.vb`, `ConvertToOre.vb`, `frmReprocessingPlant.vb`, `frmIndustryBeltFlip.vb`, `frmIceBeltFlip.vb`.

**Dependency note:** this phase should consume the market abstractions from Phase 6 rather than duplicating price lookup logic.

### Phase 9 — `EVE.IPH.Domain.ShoppingList`

**Objective:** extract material aggregation, deduplication, and build-vs-buy rollups into a reusable service layer.

**Current progress:** this phase is now complete for the current modernization milestone. The modern shopping-list slice includes legacy-compatible aggregation identity, repository-backed loading/saving for the reduced persisted list, build-vs-buy projections, invention/copy/final-item list views, and on-hand subtraction with focused tests.

**Planned first models/services:**

- `ShoppingListItem`, `MaterialList`, `AggregatedShoppingList`
- `IShoppingListService`

**Implemented behaviour:**

- Aggregate and deduplicate line items using the legacy `MaterialName + GroupName + ItemME` identity, now extended with explicit item category to keep buy/build/invention/copy/final rows distinct
- Load and save the reduced persisted shopping-list view through `IShoppingListRepository`
- Split a shopping list into buy, build, invention, copy, and final-item projections
- Apply on-hand material and on-hand component subtraction to the buy/build projections
- Fail fast when an in-memory aggregate cannot be losslessly persisted through the current `TYPE_ID`-keyed schema

**Tests:**

- Aggregation and duplicate-merge coverage
- Partial and full removal coverage
- Repository-backed load/save orchestration coverage
- Build/buy/invention/copy/final-item projection coverage
- On-hand subtraction coverage

**Primary legacy files:** `ShoppingList.vb`, `Material.vb`, `Materials.vb`, `BuildBuyItems.vb`, `frmShoppingList.vb`.

**Dependency note:** this phase should build on Phase 7 manufacturing decisions and Phase 6 market prices, while reusing the existing shopping-list persistence already present in Phase 2.

### Phase 10 — `EVE.IPH.Domain.Assets` and `EVE.IPH.Domain.Industry`

**Objective:** extract asset inventory, industry jobs, and remaining datacore/agent workflows into testable domain services.

**Planned first models/services:**

- `Asset`, `AssetLocation`, `IndustryJob`, `DatacoreAgent`
- `IAssetService`
- `IIndustryJobService`

**Primary legacy files:** `EVEAssets.vb`, `AssetViewer.vb`, `frmAssetsViewer.vb`, `EVEIndustryJobs.vb`, `frmIndustryJobsViewer.vb`, datacore-related sections of `frmMain.vb`.

**Dependency note:** Research-agent persistence is already present from Phase 5, so this phase should extend rather than replace that groundwork.

### Phase 11 — `EVE.IPH.UI.Avalonia`

**Objective:** stand up the cross-platform Avalonia host and migrate the new domain services into MVVM-based UI flows.

**Planned first slice:**

- application shell and DI composition root
- one high-value screen backed only by the new services, starting with manufacturing or market price lookup

**Constraint:** do not begin meaningful UI work until at least one calculation-heavy domain after market is complete, otherwise the UI host will outpace the extracted logic.

### Phase 12 — Legacy Decommissioning

**Objective:** retire the VB.NET WinForms application only after functional parity and migration confidence are established.

**Exit criteria before starting:**

- manufacturing, reprocessing, shopping list, assets, and industry workflows are running through the new libraries
- Avalonia UI can replace the legacy primary user journeys
- remaining legacy-only data flows are either migrated or intentionally dropped
- regression fixtures exist for the highest-risk formulas

---

## Definition of Done for this Milestone

| Criterion | How to verify |
| --- | --- |
| `Domain.Manufacturing` builds cleanly on top of the completed character and market foundations | `dotnet build .\\EVE-IPH-Modern.slnx --configuration Release` exits 0 |
| New manufacturing services are covered by executable tests | `dotnet test .\\EVE-IPH-Modern.slnx --configuration Release` stays green and adds real coverage in `EVE.IPH.Domain.Manufacturing.Tests` |
| Manufacturing formulas match known-good legacy scenarios for the extracted slice | regression fixtures from legacy outputs pass in unit tests |
| No new global mutable state is introduced | Code review — no shared mutable `static` state in new projects |
| Nullable reference types remain satisfied | Build output contains zero nullable warnings in the newly implemented projects |
| Manufacturing services can run without UI code | service tests instantiate dependencies directly without Avalonia or WinForms |
| Manufacturing uses existing modern seams instead of reintroducing legacy coupling | calculators consume repositories, character snapshots, and market services from the modern projects |

---

## Suggested Work Order

1. **Phase 7 next** — extract manufacturing calculations on top of the completed character and market services.
2. **Phase 8 after that** — reuse the market abstractions for reprocessing and mining logic rather than adding any new price-retrieval code.
3. **Phase 10 next** — pull assets and industry jobs into testable services so the Avalonia host consumes modern APIs instead of legacy code.
4. **Phase 11 after that** — start the Avalonia host only once the remaining domain-heavy workflows have modern service seams.
5. Add CI once the next domain slice is underway so future work is gated by automated builds and tests.

---

## Key Legacy Files to Study Before Each Phase

| Phase | Legacy files to read and understand |
| --- | --- |
| Phase 4 | `ESI.vb`, `Character.vb`, `EVESkillList.vb`, `EVENPCStandings.vb` |
| Phase 5 | `Character.vb`, `Corporation.vb`, `EVESkillList.vb`, `EVEResearchAgents.vb`, `EVENPCStandings.vb` |
| Phase 6 | `CacheBox.vb`, `EVEMarketer.vb`, `FuzzworksMarket.vb`, market-related sections of `ESI.vb` |
| Phase 7 | `Blueprint.vb`, `ManufacturingFacility.vb`, manufacturing sections of `frmMain.vb`, `frmBlueprintManagement.vb`, `frmBlueprintList.vb` |
| Phase 8 | `ReprocessingPlant.vb`, `ConvertToOre.vb`, `frmReprocessingPlant.vb`, `frmIndustryBeltFlip.vb`, `frmIceBeltFlip.vb` |
| Phase 9 | `ShoppingList.vb`, `Material.vb`, `Materials.vb`, `BuildBuyItems.vb`, `frmShoppingList.vb` |
| Phase 10 | `EVEAssets.vb`, `AssetViewer.vb`, `frmAssetsViewer.vb`, `EVEIndustryJobs.vb`, `frmIndustryJobsViewer.vb`, datacore sections of `frmMain.vb` |
| Phase 11 | current UI-heavy flows in `frmMain.vb` plus the related viewer/editor forms that survive Phases 7–10 |

---

## Out of Scope for This Milestone

- Avalonia UI work (Phase 11)
- Reprocessing calculations (Phase 8)
- Assets and industry jobs extraction (Phase 10)
- Legacy project retirement work (Phase 12)
- Loyalty Points domain extraction, which remains an additional legacy area to schedule after the current core domains
