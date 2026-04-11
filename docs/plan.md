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
- **Phase 7 — Domain.Manufacturing:** the current deterministic manufacturing milestone is now complete, covering the core material/time calculator, profitability and ISK-per-hour calculation, invention planning math, copy/invention activity time and usage formulas, blueprint-level invention/copy cost rollup, blueprint versus total production-time rollup, the recursive component production-time scheduling heuristic, manufacturing facility usage, usage allocation, sale adjustment, build-skill prerequisite evaluation, the pure build-vs-buy decision seam from `GetBuildFlag`, and the composed `ManufacturingAnalysisService` flow.
- **Phase 8 — Domain.Reprocessing:** the current deterministic reprocessing milestone is now complete, covering the base reprocessing yield/output calculator, the ore-conversion optimizer over resolved ore candidate yields, and the belt-flip profitability calculator over resolved belt composition and market values.
- **Phase 9 — Domain.ShoppingList:** material aggregation, legacy-compatible duplicate merging, repository-backed load/save orchestration, build-vs-buy projections, invention/copy/final-item list views, and on-hand subtraction are now implemented for the current milestone.

**Verified state:**

- `dotnet test .\\EVE-IPH-Modern.slnx --configuration Release` passes.
- The current test suite contains **304 passing tests**, including focused `Domain.Assets`, `Domain.Industry`, `Domain.Manufacturing`, `Domain.Reprocessing`, `Domain.ShoppingList`, `Domain.Characters`, `Infrastructure.ESI`, `Infrastructure.Data`, `Infrastructure.Settings`, and `Domain.Market` coverage.

**Completed or substantially complete:**

- **Phase 10 — Domain.Assets / Domain.Industry:** this phase is now complete for the current pre-Avalonia milestone. `Domain.Industry` now covers legacy-compatible job-state classification, current-job manufacturing/research/reaction summarization, character-scoped refresh/load orchestration, corporation-scoped refresh filtered to known corporation installers, and presentation-row shaping for the legacy industry jobs viewer. `Domain.Assets` now covers repository-backed asset snapshot hydration from stored asset rows plus type/location metadata, deterministic display-formatting, tree projection, filtered material-asset ancestry selection, and non-UI asset-view filtering rules for account scope, blueprint-copy subsets, search text, and sort order. The remaining datacore/research-agent value shaping has also been lifted onto the modern Phase 5 research-agent model.

**Not yet implemented in a meaningful way:**

- `EVE.IPH.UI.Avalonia`

`EVE.IPH.UI.Avalonia` still exists mainly as a project shell. `EVE.IPH.Domain.Assets`, `EVE.IPH.Domain.Industry`, `EVE.IPH.Domain.Manufacturing`, `EVE.IPH.Domain.Reprocessing`, and `EVE.IPH.Domain.ShoppingList` now have focused executable coverage and no longer belong in the shell-only category.

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

- The Avalonia UI host project exists but does not contain application code.

---

## Next Milestone: Phase 11 Avalonia Host

**Goal:** start wiring the first meaningful Avalonia UI screens against the now-complete Phase 10 domain surfaces.

**Why this is next:** the remaining high-value asset, industry-job, and datacore seams are now extracted into testable domain services, so the next useful risk-reduction step is to prove those APIs in a real cross-platform UI shell instead of continuing to deepen the domain layer in isolation.

**Current baseline for the milestone:**

- `Domain.Industry` now covers state classification, current-job summarization, character refresh/load orchestration, corporation refresh/load orchestration, and stable presentation-row shaping for the legacy industry jobs viewer.
- `Domain.Assets` now covers snapshot hydration, deterministic display-formatting, tree projection, ancestor-chain filtering, and pure asset-view filtering over owner scope, item subsets, search text, and sort order.
- `Domain.Characters` now includes datacore valuation shaping on top of the existing research-agent model.
- The full modern solution is green at **304 passing tests**.

**Concrete Phase 11 plan:**

1. **Avalonia composition root:** register the modern repositories, infrastructure adapters, and Phase 5-10 domain services in the Avalonia host and prove startup without legacy forms.
2. **First read-only screens:** wire assets, industry jobs, and research-agent/datacore views against the new domain services so the UI consumes modern APIs instead of VB logic.
3. **Navigation and shell state:** add the first real window, navigation structure, and loading/error state handling.
4. **Incremental screen replacement:** move the next highest-value tabs over one at a time while keeping the legacy application available as the fallback reference.

**Primary files to study next:** `App.axaml.cs`, the Avalonia shell project, and the newly completed Phase 10 domain services before reaching back into legacy view code.

**Definition of ready to continue Phase 11:** the Avalonia host can render the first feature-backed screens using only modern services, and legacy VB files are no longer required to power those views.

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

**Current progress:** this phase is complete for the current deterministic manufacturing milestone. It now includes eleven pure calculation seams lifted from the legacy `Blueprint.vb` logic plus two higher-order pure services: the run/material/time calculator, the final profitability calculator that derives raw/component cost, profit percent, and ISK-per-hour once the material and timing inputs are known, the invention planning calculator that derives invention chance, invented runs, job counts, sessions, and per-run invention cost from decryptor and skill inputs, the copy/invention activity calculator that derives copy time, invention time, and per-run installation usage from facility and skill inputs, the cost rollup calculator that converts those per-run invention/copy values into legacy-compatible blueprint-level invention cost, copy cost, and total usage, the timeline rollup calculator that converts base blueprint time plus copy/invention/component time into the final blueprint and total production timelines, the component scheduling calculator that reproduces the legacy recursive line-packing heuristic used to estimate total component production time, the manufacturing facility-usage calculator that reproduces the legacy `EIV * (index bonuses + facility tax + SCC + alpha)` cost formula including the Fulcrum pirate surcharge reduction and reaction-routing branch, the usage-allocation calculator that reproduces the `SetPriceData` branch for combining main manufacturing usage, component usage, carried reaction usage, and optional reprocessing usage, the sale-adjustment calculator that reproduces the legacy `AdjustPriceforTaxesandFees` deduction logic for excess-sale proceeds including optional sales tax, standard or special broker fee calculation, and the minimum 100 ISK broker fee floor, the build-vs-buy decision calculator that reproduces the deterministic `GetBuildFlag` branch over cost comparison, new-request defaults, market-insufficiency override, and manual override inputs, `ManufacturingAnalysisService`, which composes those calculators into a single domain-facing manufacturing analysis result from already-resolved inputs, and `ManufacturingPrerequisiteService`, which evaluates build-skill requirements and advanced-manufacturing time-reduction multipliers from resolved character skill levels.

**Implemented slices:**

- `ManufacturingJobInput`
- `ManufacturingJobResult`
- `ManufacturingJobCalculator`
- `ManufacturingProfitabilityInput`
- `ManufacturingProfitabilityResult`
- `ManufacturingProfitabilityCalculator`
- `InventionDecryptorModifier`
- `InventionPlanInput`
- `InventionPlanResult`
- `InventionCalculator`
- `ManufacturingActivityInput`
- `ManufacturingActivityResult`
- `ManufacturingActivityCalculator`
- `ManufacturingCostInput`
- `ManufacturingCostResult`
- `ManufacturingCostCalculator`
- `ManufacturingTimelineInput`
- `ManufacturingTimelineResult`
- `ManufacturingTimelineCalculator`
- `ComponentProductionScheduleInput`
- `ComponentProductionScheduleResult`
- `ComponentProductionScheduleCalculator`
- `ManufacturingAnalysisInput`
- `ManufacturingAnalysisResult`
- `ManufacturingAnalysisService`
- `ManufacturingSkillRequirement`
- `ManufacturingPrerequisiteInput`
- `ManufacturingPrerequisiteResult`
- `ManufacturingPrerequisiteService`
- `ManufacturingFacilityUsageInput`
- `ManufacturingFacilityUsageResult`
- `ManufacturingFacilityUsageCalculator`
- `ManufacturingUsageAllocationInput`
- `ManufacturingUsageAllocationResult`
- `ManufacturingUsageAllocationCalculator`
- `SalesBrokerFeeMode`
- `ManufacturingSaleAdjustmentInput`
- `ManufacturingSaleAdjustmentResult`
- `ManufacturingSaleAdjustmentCalculator`
- `ManufacturingBuildBuyInput`
- `ManufacturingBuildBuyResult`
- `ManufacturingBuildBuyDecider`

**Focused validation:**

- `dotnet test .\tests\EVE.IPH.Domain.Manufacturing.Tests\EVE.IPH.Domain.Manufacturing.Tests.csproj --configuration Release` passes with 54 tests.

**Deferred follow-on work beyond this milestone:**

- repository-backed manufacturing workflows that resolve the already-modeled inputs from SDE, settings, character data, and market snapshots
- broader domain-facing manufacturing orchestration and any interfaces that prove useful once those repository-backed workflows are introduced
- any later facility- or blueprint-rich abstractions that are justified by composition needs rather than by the old phase-7 design sketch

**Dependencies already available:**

- SDE blueprint/item repositories from Phase 2
- Character skills and tax inputs from Phase 5
- Provider-backed market prices from Phase 6

**Milestone exit status:**

- focused manufacturing coverage is green with 54 tests
- the full modern solution is green with 293 tests
- the deterministic manufacturing calculation seams targeted for this milestone are now extracted and documented

### Phase 8 — `EVE.IPH.Domain.Reprocessing`

**Objective:** extract ore conversion, refining yield, and belt-flip profitability into a standalone domain library.

**Current progress:** this phase is complete for the current deterministic reprocessing milestone. It now includes the base reprocessing yield/output seam lifted from `ReprocessingPlant.Reprocess`, the ore-conversion optimizer lifted from the `ConvertToOre` LP branch but expressed over already-resolved ore candidate yields and objective values, and the belt-flip profitability seam lifted from `frmIndustryBeltFlip.vb` and `frmIceBeltFlip.vb` over already-resolved belt lines, sale values, mining rates, compression outputs, and reprocessing usage.

**Implemented slices:**

- `ReprocessingCalculationInput`
- `ReprocessingCalculationResult`
- `ReprocessingCalculator`
- `OreConversionInput`
- `OreConversionRequirement`
- `OreConversionCandidate`
- `OreConversionYield`
- `OreConversionSelection`
- `OreConversionExcessMaterial`
- `OreConversionResult`
- `OreConversionOptimizer`
- `BeltFlipInput`
- `BeltFlipLineInput`
- `BeltFlipOutcome`
- `BeltFlipResult`
- `BeltFlipCalculator`

**Focused validation:**

- `dotnet test .\tests\EVE.IPH.Domain.Reprocessing.Tests\EVE.IPH.Domain.Reprocessing.Tests.csproj --configuration Release` passes with 12 tests.

**Deferred follow-on work beyond this milestone:**

- repository-backed ore, ice, and refined-material candidate resolution from SDE and market data
- higher-level orchestration that turns facility settings, character skills, and selected ore groups into the already-resolved pure inputs used by the current services
- any later UI-facing or persistence-backed workflows that consume those reprocessing services

**Milestone exit status:**

- focused reprocessing coverage is green with 12 tests
- the full modern solution is green with 293 tests
- the deterministic reprocessing calculation seams targeted for this milestone are now extracted and documented

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

**Current progress:** this phase is now complete for the current milestone. The modern industry code covers legacy-compatible industry-job state classification (`Pending`, `In Progress`, `Complete`, etc.), current-job manufacturing/research/reaction summarization based on the legacy `activityID` rules, character-scoped and corporation-scoped job refresh/load orchestration while preserving the legacy `9 -> 11` reaction normalization, installer filtering for corporation jobs, and stable presentation shaping for the legacy industry jobs viewer. The modern assets code now covers repository-backed asset snapshot hydration from stored asset rows plus type/location metadata, the pure location/item display projection rules used by the legacy asset tree, blueprint copy/original text, industry-job text suffixes, stacked-quantity formatting, the solar-system location suffix for `Space` and `Ship Offline` flags, the structural tree projection rules for base-location nodes, synthetic container nodes, item parentage, the filtered ancestor-chain lookup used to build material-specific asset subsets, and the non-UI asset-view filtering rules for owner scope, blueprint-copy subsets, search text, and sort order. The remaining datacore workflow from the research-agents view is also lifted into a modern datacore valuation service on top of the Phase 5 research-agent model.

**Planned first models/services:**

- `Asset`, `AssetLocation`, `IndustryJob`, `DatacoreAgent`
- `IAssetService`
- `IIndustryJobService`

**Implemented first slice:**

- `AssetDisplayItem`
- `AssetBlueprintKind`
- `IAssetDisplayFormatter`
- `AssetDisplayFormatter`
- `AssetTreeItem`
- `AssetTreeNode`
- `AssetTreeNodeKind`
- `IAssetTreeProjector`
- `AssetTreeProjector`
- `AssetHierarchyItem`
- `IAssetHierarchyService`
- `AssetHierarchyService`
- `IndustryJob`
- `IndustryJobSnapshot`
- `CorporationIndustryJobSnapshot`
- `IndustryJobState`
- `IndustryJobSummary`
- `ICharacterIndustryJobService`
- `CharacterIndustryJobService`
- `ICorporationIndustryJobService`
- `CorporationIndustryJobService`
- `IIndustryJobService`
- `IndustryJobService`
- `IndustryJobViewItem`
- `IndustryJobDisplayRow`
- `IIndustryJobPresentationService`
- `IndustryJobPresentationService`
- `AssetRecord`
- `AssetTypeMetadata`
- `AssetLocationMetadata`
- `HydratedAsset`
- `AssetViewRequest`
- `AssetSortMode`
- `IAssetSnapshotHydrator`
- `AssetSnapshotHydrator`
- `IAssetViewFilterService`
- `AssetViewFilterService`
- `ResearchAgentDatacoreSnapshot`
- `ResearchAgentDatacoreSummary`
- `IResearchAgentDatacoreService`
- `ResearchAgentDatacoreService`

**Orchestration groundwork added:**

- `IIndustryJobDataSource`
- `IIndustryJobRepository`
- `IndustryJobData`
- `IndustryJobRecord`
- `IndustryJobScope`

**Focused validation:**

- `dotnet test .\tests\EVE.IPH.Domain.Assets.Tests\EVE.IPH.Domain.Assets.Tests.csproj --configuration Release` passes with 22 tests.
- `dotnet test .\tests\EVE.IPH.Domain.Industry.Tests\EVE.IPH.Domain.Industry.Tests.csproj --configuration Release` passes with 19 tests.
- `dotnet test .\tests\EVE.IPH.Domain.Characters.Tests\EVE.IPH.Domain.Characters.Tests.csproj --configuration Release` passes with 22 tests.
- `dotnet test .\EVE-IPH-Modern.slnx --configuration Release` passes with 304 tests.

**Primary legacy files:** `EVEAssets.vb`, `frmAssetsViewer.vb`, `EVEIndustryJobs.vb`, `frmIndustryJobsViewer.vb`, datacore-related sections of `frmMain.vb`.

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
| Avalonia host starts with the modern composition root only | `dotnet build .\\EVE-IPH-Modern.slnx --configuration Release` exits 0 and the Avalonia app can launch without depending on legacy WinForms code |
| The first Avalonia screens consume only modern services | assets, industry jobs, and datacore/research-agent views resolve data through `Domain.Assets`, `Domain.Industry`, and `Domain.Characters` services rather than VB code |
| The Phase 10 domain baseline remains stable while UI work lands | `dotnet test .\\EVE-IPH-Modern.slnx --configuration Release` stays green at or above the current 304 passing tests |
| No new global mutable state is introduced | Code review — no shared mutable `static` state in new projects |
| Nullable reference types remain satisfied | Build output contains zero nullable warnings in the newly implemented projects |
| New UI work does not reintroduce legacy coupling | ViewModels and shell composition consume repositories, settings, and domain services from the modern projects only |
| New screens remain testable without UI-specific logic leaking into the domain layer | domain/service tests instantiate dependencies directly without Avalonia or WinForms |

---

## Suggested Work Order

1. **Phase 11 next** — start the Avalonia host now that the remaining asset, industry-job, and datacore seams have modern service coverage.
2. Wire the first read-only screens against the completed Phase 10 services before adding broader UI state or workflows.
3. Expand the CI workflow once the Avalonia host starts landing so future work is gated by broader automated checks.

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

- Additional domain extraction beyond the completed Phase 10 baseline unless UI work exposes a concrete missing seam
- Legacy project retirement work (Phase 12)
- Loyalty Points domain extraction, which remains an additional legacy area to schedule after the current core domains
