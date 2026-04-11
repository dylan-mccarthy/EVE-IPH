# EVE-IPH Modernisation — Next Milestone Plan

## Current State (as of April 2026)

The repository has moved beyond the initial scaffold. The modern solution exists, builds, and includes implemented C# code plus passing tests for the shared foundation layers.

**Completed or substantially complete:**

- **Phase 0 — Groundwork:** `EVE-IPH-Modern.slnx`, all planned `src/` and `tests/` projects, `Directory.Build.props`, and `.editorconfig` are present.
- **Phase 1 — Domain.Core:** identifier value objects, `Result<T>`, `Maybe<T>`, `Error`, core enumerations, and repository/settings interfaces are implemented.
- **Phase 2 — Infrastructure.Data:** SQLite connection factory, migration runner, and repository implementations for SDE/app data access are implemented.
- **Phase 3 — Infrastructure.Settings:** JSON settings store, platform-specific storage path handling, and XML-to-JSON migration support are implemented.
- **Phase 4 — Infrastructure.ESI:** typed ESI HTTP client, retry/error-limit handling, PKCE authorization request generation, token exchange/refresh, interactive localhost callback flow, and file-based token persistence are implemented.

**Verified state:**

- `dotnet test .\\EVE-IPH-Modern.slnx --configuration Release` passes.
- The current test suite contains **132 passing tests**, including focused coverage for `Infrastructure.ESI`.

**Not yet implemented in a meaningful way:**

- `EVE.IPH.Domain.Characters`
- `EVE.IPH.Domain.Market`
- `EVE.IPH.Domain.Manufacturing`
- `EVE.IPH.Domain.Reprocessing`
- `EVE.IPH.Domain.ShoppingList`
- `EVE.IPH.Domain.Assets`
- `EVE.IPH.Domain.Industry`
- `EVE.IPH.UI.Avalonia`

These later projects currently exist as project shells only, and several matching test projects also exist without real test coverage yet.

**Known gaps relative to the original Phase 0 checklist:**

- No GitHub Actions workflow is present yet.
- The Avalonia UI host project exists but does not contain application code.

---

## Next Milestone: Domain Service Extraction (Phases 5 → 6)

**Goal:** Build the first domain-level services on top of the completed foundation and ESI infrastructure: character services first, then market services. This milestone should produce the first end-to-end business behaviour in the modern codebase while still avoiding UI work.

---

### Phase 5 — `EVE.IPH.Domain.Characters` (character, skills, standings)

Once ESI infrastructure exists, the first real domain service layer can be extracted.

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

#### 6.1 Models

- `MarketPrice`
- `PriceHistory`
- `MarketOrder`

#### 6.2 Services

- `IMarketPriceProvider` implementations for ESI, EVEMarketer, and Fuzzworks
- Caching decorator using `IMarketCacheRepository`
- `IMarketService` to orchestrate provider selection and batch lookups

#### 6.3 Behaviour to Implement

- Batch price retrieval by type ID
- Cache expiry rules migrated from `CacheBox.vb`
- Provider selection driven by settings

#### 6.4 Tests

- Cache hit/miss and expiry tests
- Provider selection tests
- Price aggregation tests

> **Reference files from legacy app:** `CacheBox.vb`, `EVEMarketer.vb`, `FuzzworksMarket.vb`, market-related code in `ESI.vb`

---

## Definition of Done for this Milestone

| Criterion | How to verify |
| --- | --- |
| `Domain.Characters` and `Domain.Market` build cleanly on top of the completed ESI infrastructure | `dotnet build .\\EVE-IPH-Modern.slnx --configuration Release` exits 0 |
| New domain services are covered by executable tests | `dotnet test .\\EVE-IPH-Modern.slnx --configuration Release` stays green and adds real coverage in the previously empty test projects |
| No new global mutable state is introduced | Code review — no shared mutable `static` state in new projects |
| Nullable reference types remain satisfied | Build output contains zero nullable warnings in the newly implemented projects |
| Character and market services can run without UI code | Service tests instantiate dependencies directly without Avalonia or WinForms |
| Character services exercise real ESI-backed workflows | Tests cover character load, skill lookups, and standings-derived calculations using the new ESI abstractions |

---

## Suggested Work Order

1. **Phase 5 first** — character services are now the narrowest domain slice that can exercise the completed ESI infrastructure end to end.
2. **Phase 6 next** — market services unlock later manufacturing, reprocessing, and shopping list work.
3. Add CI once these slices are in place so future domain work is gated by automated builds and tests.

---

## Key Legacy Files to Study Before Each Phase

| Phase | Legacy files to read and understand |
| --- | --- |
| Phase 4 | `ESI.vb`, `Character.vb`, `EVESkillList.vb`, `EVENPCStandings.vb` |
| Phase 5 | `Character.vb`, `Corporation.vb`, `EVESkillList.vb`, `EVEResearchAgents.vb`, `EVENPCStandings.vb` |
| Phase 6 | `CacheBox.vb`, `EVEMarketer.vb`, `FuzzworksMarket.vb`, market-related sections of `ESI.vb` |

---

## Out of Scope for This Milestone

- Avalonia UI work (Phase 11)
- Manufacturing calculations (Phase 7)
- Reprocessing calculations (Phase 8)
- Shopping list logic (Phase 9)
- Assets and industry jobs extraction (Phase 10)
- Legacy project retirement work (Phase 12)
- Loyalty Points domain extraction, which remains an additional legacy area to schedule after the current core domains
