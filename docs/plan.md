# EVE-IPH Modernisation — Next Milestone Plan

## Current State (as of April 2026)

Phase 0 (Groundwork) is **complete**. The solution skeleton `EVE-IPH-Modern.sln` exists with all twelve projects scaffolded and `Directory.Build.props` in place. No C# implementation files have been written yet — every project is an empty shell.

The latest master changes have been merged in. Notable additions from master include:
- `EVELoyaltyPoints.vb` (new LP-store domain area)
- Significant refactoring of `ManufacturingFacility.vb` (~5,200 lines), `frmMain.vb` (~21,800 lines), `ShoppingList.vb`, and `frmConversiontoOreSettings.vb`
- Removal of `frmManualPriceUpdate` (replaced by in-line market history refresh)
- Updated ESI scopes in `ESI.vb`

---

## Next Milestone: Foundation Phases (Phases 1 → 3)

**Goal:** Deliver a fully tested, dependency-injected foundation — shared types, data access, and settings — that every subsequent phase will build on. No UI work yet; only libraries and tests.

---

### Phase 1 — `EVE.IPH.Domain.Core` (Shared Types & Interfaces)

All later phases depend on this. Do it first and keep it lean.

#### 1.1 Value Objects
Define strongly typed identifiers as C# `readonly record struct` to prevent primitive obsession:
- `TypeId`, `ItemId`, `BlueprintId`, `RegionId`, `SystemId`, `StationId`
- `CharacterId`, `CorporationId`, `AllianceId`

#### 1.2 Result & Option Types
- `Result<T>` — discriminated union of success/failure; wraps a value or an `Error` record
- `Error` — record holding `Code` (string) and `Message` (string)
- `Maybe<T>` — option type for nullable domain values (avoids nullable reference proliferation in return types)

#### 1.3 Domain Enumerations
- `ActivityType`: `Manufacturing`, `Copying`, `ResearchME`, `ResearchTE`, `Invention`, `Reaction`, `SimpleReaction`
- `TechLevel`: `T1`, `T2`, `T3`, `Faction`, `Structure`
- `MarketPriceType`: `Sell`, `Buy`, `Jita4Sell`, `Jita4Buy`, `Average`
- `ReprocessingOreType`: `Raw`, `Compressed`, `MoonOre`, `Ice`

#### 1.4 Core Interfaces (contracts implemented by Infrastructure)
- `IMarketPriceSource` — fetch a batch of prices by TypeId
- `IBlueprintRepository` — read blueprint materials and attributes from SDE
- `IItemRepository` — read item names, groups, categories from SDE
- `ICharacterRepository` — persist and retrieve character records
- `ISettingsStore` — generic typed settings read/write

#### 1.5 Tests
- Value type equality, comparison, and ToString for all identifiers
- `Result<T>` map, bind, and match exhaustiveness
- `Maybe<T>` map and default fallback behaviour

---

### Phase 2 — `EVE.IPH.Infrastructure.Data` (SQLite Repositories)

Replaces the global `EVEDB` singleton and raw SQL strings scattered across `DBConnection.vb` and every form file.

#### 2.1 Connection Factory
- `IDbConnectionFactory` with a `SqliteConnectionFactory` implementation backed by `Microsoft.Data.Sqlite`
- Connection string built from platform-appropriate app-data path

#### 2.2 SDE Read Repositories (Dapper-based, read-only)
These query the unmodified SDE SQLite file shipped with each EVE release:
- `SqliteBlueprintRepository : IBlueprintRepository` — materials, time, skills for each blueprint activity
- `SqliteItemRepository : IItemRepository` — `invTypes`, `invGroups`, `invCategories`
- `SqliteRegionRepository : IRegionRepository` — `mapRegions`, `mapSolarSystems`, `staStations`
- `SqliteAttributeRepository` — `dgmTypeAttributes` (for ship/structure bonuses)

#### 2.3 Application Write Repositories
These query/update the application's own database (separate file from the SDE):
- `SqliteCharacterRepository : ICharacterRepository` — stored characters, OAuth tokens (encrypted)
- `SqliteOwnedBlueprintRepository : IOwnedBlueprintRepository` — user's ME/TE-researched blueprints
- `SqliteMarketCacheRepository : IMarketCacheRepository` — cached market prices with expiry timestamps
- `SqliteShoppingListRepository : IShoppingListRepository` — persisted shopping lists

#### 2.4 Schema Migration Runner
- Simple versioned migration runner that applies numbered SQL scripts on startup
- Migrations stored as embedded resources in the project
- No breaking changes to schema that the legacy app reads

#### 2.5 Tests
- Integration tests using an in-memory SQLite database seeded with a minimal SDE snapshot
- Round-trip tests for each write repository (insert → fetch → assert equality)
- Migration idempotency test (running migrations twice does not error)

> **Reference files from legacy app:** `DBConnection.vb`, `Globals.vb` (EVEDB usages), `EVEBlueprints.vb`, `EVEAssets.vb`, `EVEIndustryJobs.vb`

---

### Phase 3 — `EVE.IPH.Infrastructure.Settings` (JSON Settings)

Replaces `ProgramSettings.vb` XML serialisation and `Settings.vb` with a versioned, cross-platform JSON store.

#### 3.1 Settings Models
Define plain C# records, one per logical group matching existing VB settings classes:
- `ApplicationSettings` — window layout, last-used character, default market region
- `ManufacturingSettings` — default facility, skill overrides, component build flags
- `MarketSettings` — price source, cache TTL, region preferences
- `ReprocessingSettings` — default plant efficiency, skill overrides
- `UpdaterSettings` — auto-update channel, last-checked timestamp

#### 3.2 `JsonSettingsStore : ISettingsStore`
- Generic `ReadAsync<T>` / `WriteAsync<T>` backed by `System.Text.Json`
- Per-platform base path: `%APPDATA%\EVE-IPH` (Windows), `~/Library/Application Support/EVE-IPH` (macOS), `~/.config/eve-iph` (Linux)
- Atomic write pattern (write to `.tmp`, then rename) to prevent corruption on crash
- JSON schema version field in each file for forward-compatible migration

#### 3.3 Legacy Migration
- One-off migrator that reads the existing XML settings file on first run and writes JSON equivalents
- Migrator is a distinct class; it is called once at startup and then disabled

#### 3.4 Tests
- Serialisation round-trip for each settings record (all fields preserved)
- Migration: given a legacy XML snapshot, output JSON matches expected record
- Atomic write: simulate crash mid-write, assert previous settings remain intact
- Platform path resolution: mock `Environment.GetFolderPath` for each platform

> **Reference files from legacy app:** `ProgramSettings.vb`, `Settings.vb`

---

## Definition of Done for this Milestone

| Criterion | How to verify |
|---|---|
| All three projects build without warnings | `dotnet build EVE-IPH-Modern.sln --configuration Release` exits 0 |
| Every public type and method has at least one test | `dotnet test` — all green; coverage report shows >80% on Domain.Core and Infrastructure.* |
| No global mutable state in new projects | Code review — no `static` fields that hold state |
| Nullable reference types fully satisfied | Build output contains zero nullable warnings |
| Schema changes are backward compatible | Legacy VB app still reads the SQLite file without errors after migrations run |
| Settings migration runs cleanly | Integration test: seed XML file → run migrator → JSON output matches expected values |

---

## Suggested Work Order

1. **Phase 1 first** — it has no dependencies and unblocks Phases 2, 3, and all domain phases.
2. **Phase 2 and Phase 3 in parallel** — they both depend only on Phase 1 interfaces and are independent of each other.
3. Once all three are green in CI, open a PR to merge the milestone branch into the main development branch.
4. Begin Phase 4 (ESI Infrastructure) and Phase 5 (Character Domain) in the next milestone.

---

## Key Legacy Files to Study Before Each Phase

| Phase | Legacy files to read and understand |
|---|---|
| Phase 1 | `Blueprint.vb`, `Character.vb`, `Material.vb`, `Globals.vb` (type constants) |
| Phase 2 | `DBConnection.vb`, `EVEBlueprints.vb`, `EVEAssets.vb`, `EVEIndustryJobs.vb`, `EVENPCStandings.vb` |
| Phase 3 | `ProgramSettings.vb`, `Settings.vb`, `app.config` |

---

## Out of Scope for This Milestone

- ESI HTTP client (Phase 4)
- Character domain services (Phase 5)
- Any Avalonia UI work (Phase 11)
- Manufacturing calculations (Phase 7)
- Loyalty Points domain (added in latest master — will be Phase 10 extension)
