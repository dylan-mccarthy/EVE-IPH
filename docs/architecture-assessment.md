# EVE Isk per Hour — Architecture Assessment

## 1. Overview

EVE Isk per Hour (EVE-IPH) is a desktop application for the game EVE Online that helps players calculate the ISK-per-hour profitability of industry activities: manufacturing, invention, reprocessing, mining, and datacores. It connects to the EVE Swagger Interface (ESI) to pull live character, market, and universe data, and maintains a local SQLite database that mirrors the Static Data Export (SDE) shipped with each EVE Online expansion release.

---

## 2. Technology Stack

| Concern | Technology |
|---|---|
| Language | Visual Basic .NET |
| Runtime | .NET Framework 4.6.1 |
| UI Framework | Windows Forms (WinForms) |
| Database | SQLite (via `System.Data.SQLite`) |
| HTTP / EVE API | Custom `ESI` class using `System.Net.HttpWebRequest` |
| Market data (secondary) | EVEMarketer API, Fuzzworks Market API |
| Serialisation | Newtonsoft.Json |
| Linear programming | LpSolveDotNet (x86 native) |
| Packaging | XCOPY / custom XML-based self-updater |
| Platform | Windows-only, x86 |

---

## 3. Source File Inventory

The repository is a single Visual Studio project (`EVE Isk per Hour.vbproj`) containing approximately **109,000 lines of VB.NET** across:

| Category | Count | Notable files |
|---|---|---|
| WinForms form files (`.vb` + `.Designer.vb` + `.resx`) | ~47 forms | `frmMain.vb` (21,945 lines), `frmUpwellStructureFitting.vb`, `frmShoppingList.vb` |
| Domain / logic modules | ~40 files | `Blueprint.vb`, `Character.vb`, `ManufacturingFacility.vb`, `ShoppingList.vb`, `Material.vb`, `ReprocessingPlant.vb` |
| Data access | 1 | `DBConnection.vb` |
| ESI / network | 3 | `ESI.vb`, `EVEMarketer.vb`, `FuzzworksMarket.vb` |
| Global state | 2 | `Globals.vb`, `ProgramSettings.vb` |
| Infrastructure | 4 | `CacheBox.vb`, `ThreadingArray.vb`, `ProgramUpdater.vb`, `StructureProcessor.vb` |
| EVE data models | 6 | `EVEAssets.vb`, `EVEBlueprints.vb`, `EVEIndustryJobs.vb`, `EVENPCStandings.vb`, `EVEResearchAgents.vb`, `EVESkillList.vb` |
| UI helpers / controls | 5 | `MyListView.vb`, `ManufacturingListView.vb`, `ManufacturingFacility.vb` (user control), `MyDomainUpDown.vb`, `TimePicker.vb` |
| Settings | 2 | `Settings.vb`, `ProgramSettings.vb` |

---

## 4. Architectural Style

The application is a **monolithic, single-project Windows Forms application** with no architectural layering enforced by the project structure. It exhibits the following characteristics:

### 4.1 Global Mutable State
`Globals.vb` (`Public_Variables` module) and `ProgramSettings.vb` (`SettingsVariables` module) expose shared mutable state to the entire application — database connections, the selected character, active blueprint, file paths, ESI error handler, and all user settings. Any form or class can read or write these at any time.

### 4.2 God Object — `frmMain.vb`
At nearly 22,000 lines, `frmMain` is the hub of the application. It acts simultaneously as:
- The main window host (tabs for Manufacturing, Mining, Prices, Datacores, Research Agents, Industry Jobs)
- A calculation engine that drives blueprint profit calculations
- A controller for market price updates
- A coordinator for threading
- The primary event handler for all user interactions on the main tab set

### 4.3 No Separation of Concerns
Business logic, UI rendering, and data access are interleaved throughout the form files. For example, SQL queries are written inline inside form event handlers, and market calculations are performed directly within `frmMain` rather than in a service or engine class.

### 4.4 Data Access
A single `DBConnection` class wraps a `SQLiteConnection`. It is accessed as a globally shared singleton (`EVEDB` in `Globals.vb`). SQL is composed as raw strings and executed directly via `SQLiteCommand` throughout all files. There is no ORM, no repository pattern, and no parameterised query helper beyond what SQLite.NET provides natively.

### 4.5 Threading
Manual thread management is implemented via the `ThreadingArray` class, which keeps a list of `System.Threading.Thread` references and exposes `StopAllThreads()` (using `Thread.Abort()`). `Application.DoEvents()` is used to keep the UI responsive in synchronous operations, a pattern that is problematic and unsupported in modern .NET.

### 4.6 ESI (EVE API) Integration
The `ESI` class handles:
- OAuth2 PKCE authorisation flow via a local TCP listener on port 12500
- Token refresh and storage
- All character-scoped API calls (skills, assets, blueprints, standings, industry jobs)
- All public API calls (market orders, market history, structures, universe data)

ESI calls use raw `HttpWebRequest`. There is no retry policy, circuit breaker, or structured rate-limiting beyond a fixed maximum connection count.

### 4.7 Market Price Sources
Three price sources are supported and switched between at runtime:
- **ESI** (official CCP market API)
- **EVEMarketer** (third-party aggregator)
- **Fuzzworks** (third-party aggregator)

Each is implemented as its own class with duplicated HTTP request logic.

### 4.8 Settings Persistence
Settings are serialised to and from XML files on disk using `System.Xml` directly within `ProgramSettings`. Each settings class has its own serialisation logic. There is no schema versioning.

### 4.9 Self-Updater
The application ships a separate `EVEIPH Updater.exe` binary and checks an XML manifest hosted on GitHub for new versions. The main process downloads the new build and launches the updater, which replaces the running files.

---

## 5. Identified Functional Domains

Despite the lack of enforced layering, it is possible to identify coherent functional domains within the codebase:

| Domain | Key files | Responsibility |
|---|---|---|
| **Character & Authentication** | `ESI.vb`, `Character.vb`, `Corporation.vb`, `frmAddCharacter.vb`, `frmManageAccounts.vb` | ESI OAuth2 PKCE flow, token storage, character profile data |
| **Skills** | `EVESkillList.vb`, `EVEAttributes.vb`, `frmCharacterSkills.vb`, `frmReqSkills.vb` | Character skill data, overrides, display |
| **Manufacturing / Industry** | `Blueprint.vb`, `ManufacturingFacility.vb`, `frmMain.vb` (mfg section), `frmBlueprintManagement.vb`, `frmBlueprintList.vb` | Blueprint profit calculation, facility bonuses, invention, copying |
| **Market Data** | `ESI.vb` (market calls), `EVEMarketer.vb`, `FuzzworksMarket.vb`, `MarketPriceInterface.vb`, `CacheBox.vb` | Price retrieval, caching, history |
| **Reprocessing / Mining** | `ReprocessingPlant.vb`, `ConvertToOre.vb`, `frmReprocessingPlant.vb`, `frmIndustryBeltFlip.vb`, `frmIceBeltFlip.vb` | Ore processing, yield calculation, mining belt analysis |
| **Assets** | `EVEAssets.vb`, `AssetViewer.vb`, `frmAssetsViewer.vb` | Character/corp asset retrieval, display, filtering |
| **Blueprints (character-owned)** | `EVEBlueprints.vb`, `DecryptorList.vb` | Owned blueprint list, decryptors for invention |
| **Industry Jobs** | `EVEIndustryJobs.vb`, `frmIndustryJobsViewer.vb` | Active industry job tracking |
| **Shopping List** | `ShoppingList.vb`, `Material.vb`, `Materials.vb`, `BuildBuyItems.vb`, `frmShoppingList.vb` | Material aggregation, buy/build decisions, cost summary |
| **Research Agents / Datacores** | `EVEResearchAgents.vb`, `frmResearchAgents.vb`, `frmMain.vb` (datacores section) | Datacore agent IPH calculation |
| **Standings** | `EVENPCStandings.vb`, `frmCharacterStandings.vb` | NPC corporation standings for tax rate calculations |
| **Structures / Facilities** | `StructureProcessor.vb`, `frmUpwellStructureFitting.vb`, `frmViewSavedStructures.vb`, `frmAddStructureIDs.vb` | Upwell structure data, rig bonuses, manufacturing facility modelling |
| **Settings** | `ProgramSettings.vb`, `Settings.vb`, `frmSettings.vb` | All user preferences, per-tab and global settings |
| **Data Access** | `DBConnection.vb` | SQLite connection management |
| **Infrastructure** | `CacheBox.vb`, `ThreadingArray.vb`, `ProgramUpdater.vb`, `Globals.vb` | Caching, threading, self-update, global app state |

---

## 6. Key Technical Debts

| Issue | Impact |
|---|---|
| VB.NET on .NET Framework 4.6.1 | Cannot run on macOS/Linux; limited to x86; no long-term support path |
| WinForms UI | Windows-only; no cross-platform path; designer-generated code is fragile |
| 22,000-line `frmMain.vb` | Untestable, unmaintainable, impossible to reason about in isolation |
| Global mutable state (`Globals.vb`) | Hidden coupling between all components; no testability without the whole app |
| Thread management with `Thread.Abort()` | Deprecated and removed in .NET 5+; unreliable cleanup |
| `Application.DoEvents()` | Causes re-entrancy bugs and is not available in modern .NET |
| Raw SQL strings throughout the codebase | SQL injection risk for any user-controlled input; no type safety |
| x86-only build due to `LpSolveDotNet.Native.win-x86` | Blocks 64-bit and cross-platform builds |
| No unit tests | Zero automated test coverage; all verification is manual |
| No dependency injection | All dependencies are implicit globals; impossible to mock or substitute |
| Inline XML serialisation for settings | Fragile, no versioning, scattered serialisation logic |
| Self-updater via a separate executable | Complex, brittle; not compatible with any modern package delivery mechanism |
| `On Error Resume Next` used in several places | Silently swallows errors, hides bugs |

---

## 7. Summary

EVE-IPH is a feature-rich application built over many years in a style that was conventional for VB.NET WinForms development in the early .NET Framework era. It has accumulated significant technical debt: a monolithic architecture with no layer separation, heavy reliance on global state, no automated tests, and tight coupling to the Windows platform and x86 architecture. The application is entirely unmaintainable from a modern engineering standpoint and cannot be extended without risking regressions across the entire feature set.

The codebase does, however, contain well-understood domain logic — the formulas and algorithms for EVE industry profitability calculations — that can be extracted, tested in isolation, and reused in a modernised architecture.
