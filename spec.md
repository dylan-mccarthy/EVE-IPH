# EVE Isk per Hour – Current System Spec

## Overview
- Windows desktop tool (WinForms, VB.NET) that helps EVE Online players plan and optimize industrial activities (blueprints, reactions, mining, market pricing, shopping lists, structures).
- Target framework: .NET Framework 4.6.1; Output: WinExe, primarily x86 but AnyCPU configs exist.
- Core app plus two auxiliary projects: SQLite DLL updater and main updater utilities.

## Runtime Architecture
- **UI Layer:** Dozens of WinForms (`frmMain` is the hub) covering blueprints, manufacturing, market price updates, mining, assets, industry jobs, structures, shopping lists, history viewers, settings, and status dialogs. Custom controls (`ManufacturingFacility`, `ManufacturingListView`, `MyDomainUpDown`, `DoubleTrackBar`, etc.).
- **Global State:** `Globals.vb` defines extensive public constants, mutable globals, enums, and shared instances (e.g., `EVEDB`, `SelectedCharacter`, `ESIErrorHandler`, open form references). Relies heavily on module-level state instead of dependency injection.
- **Configuration & Settings:** `ProgramSettings.vb` encapsulates defaults and per-feature settings (application, blueprint tab, price updates, manufacturing, datacores, mining, shopping, column layouts, structure viewers, etc.). Settings are loaded via `AllSettings` helpers and stored under a user-writable path.
- **Data Access:** `DBConnection.vb` wraps `System.Data.SQLite` with WAL mode and manual transaction helpers. Uses file-based SQLite database `EVEIPH DB.sqlite` stored either alongside the executable or under `%AppData%/EVE IPH` depending on install layout.
- **External Services:**
  - **ESI (EVE Swagger Interface):** `ESI.vb` handles OAuth2/PKCE SSO via localhost listener (`127.0.0.1:12500`), manages scopes for character/corporation assets, blueprints, industry jobs, skills, standings, structures, and structure markets. Uses `HttpWebRequest`/`WebClient`, manual threading, and basic rate/limit handling.
  - **Market data providers:** Classes like `EVEMarketer.vb` and `FuzzworksMarket.vb` (not yet reviewed) fetch market prices for manufacturing calculations.
  - **Linear programming:** `LpSolveDotNet` referenced for optimization routines.
- **Updater Flow:** Constants for update endpoints (`LatestVersionIPH.xml`, patch notes) and helper executables (`EVEIPH Updater.exe`, SQLite DLL updater). On startup, `frmMain` optionally checks for updates and downloads into a dynamic path.
- **Threading:** Mix of manual `Thread`, `ThreadingArray`, and UI thread `Invoke` calls; some long-running operations toggle cancellation flags (`CancelUpdatePrices`, `CancelThreading`).

## Build & Packaging
- Legacy, non-SDK `.vbproj` with explicit `Compile` items and `packages.config` NuGet management. Uses `packages/` folder with pinned versions (e.g., `Newtonsoft.Json 13.0.3`, `Stub.System.Data.SQLite.Core.NetFramework 1.0.116.0`, `LpSolveDotNet 4.0.0`).
- Compiles to `Root Directory\` for x86 Debug/Release; AnyCPU/Itanium configs exist but map to x86 outputs.
- App manifest: `My Project/app.manifest`; icon `icon07_02.ico`.

## Data Model & Domains (high level)
- **Industry/Manufacturing:** Blueprint entities, material requirements, ME/TE calculations, facility modifiers, decryptors, relics, invention settings, build vs. buy rules, SVR (supply vs. demand) thresholds.
- **Mining/Reprocessing:** Ore/ice/gas yield calculations, ship fittings, upgrades, crystal usage, sovereignty filters, jump fuel costs, refinery settings (`ReprocessingPlant`, `ConvertToOre`).
- **Market Pricing:** Price profiles, region/system filters (Jita/The Forge defaults), history updates with rate-limit tracking; exports to CSV/SSV/multibuy formats.
- **Characters & Skills:** Skill overrides, implants, standings, alpha/omega handling, research agents, datacore income.
- **Assets & Shopping Lists:** Asset viewers (personal/corp), shopping list builder with export and update hooks; blueprint ownership flags (BPO/BPC/copies/invented).
- **Structures & Facilities:** Upwell structure data, bonuses popout, facility tax/fees, shared facility settings per character.

## Execution Flow (simplified)
1. `frmMain.New` initializes globals, detects install layout (single-folder vs `%AppData%`), optionally runs update checks, opens SQLite, loads user settings, and wires forms/controls.
2. User authenticates via ESI SSO; tokens are stored in memory for subsequent API calls. Market/history data fetched from providers; calculations populate multiple tabs/grids.
3. User actions drive blueprint/manufacturing calculations, shopping list generation, mining yield, and asset scans. Results cached in SQLite and in-memory structures.

## Current Constraints / Observed Pain Points
- Heavy reliance on global mutable state and cross-form coupling.
- Synchronous and manual threading patterns; limited async/await and modern HttpClient usage.
- Legacy project format with `packages.config` and explicit compile items; tied to .NET Framework 4.6.1 and x86.
- Security/network: custom localhost listener and manual PKCE; limited resilience/logging around API failures.
- Testing coverage unknown; no tests discovered.
- Large monolithic `frmMain` (~20k lines) mixes UI, domain, and data concerns.

---

## Modernization Target (Web Replacement)
The modernization effort is targeting a full replacement of the WinForms UI with:
- **Backend:** ASP.NET Core minimal APIs + SQLite
- **Frontend:** React SPA (Vite + TypeScript)

WinForms remains reference-only while parity is reached.

## Modernization Checkpoint (December 11, 2025)

### What’s Implemented (Web)
- **Auth + Character data (Phase 1–3):** SSO/PKCE, token persistence/refresh, characters + skills/assets/industry/market orders/wallet pages.
- **Blueprint search/details (Phase 4.1):** search and detailed blueprint view with activities/materials/products.
- **Market prices cache + management UI (Phase 4.2):** cached price table in SQLite, Market Data page for manual region sync (The Forge / Jita default).
- **Manufacturing calculator pricing (Phase 4.3 partial):** Blueprints calculator reads cached prices only and computes component cost/product value/profit.
- **Component → raw expansion (Phase 4.3 partial):** backend endpoint calculates raw-material breakdown by recursively expanding manufacturable components.

### Key Decisions / Tradeoffs
- **Manual market refresh only:** calculator pages never trigger ESI refresh; Market Data page is the single explicit refresh workflow.
- **Batch market sync behavior:** region “all orders” pagination is fast, but observed to frequently omit buy orders; cached `buyPrice` can be 0. Sell prices are used for manufacturing material acquisition costs.

### Parity Mapping (Legacy → Web)
This is the functional map we’re using to replace the WinForms system.

**Manufacturing**
- Legacy includes many outputs/config knobs (example surface: profit %, ISK/hr, ROI, taxes/broker fees, installation/job fees, facility/system index bonuses, price trend/SVR, invention costs).
- Web currently covers: blueprint lookup + material lists + cached sell-price costing + raw breakdown backend.
- Web still needs: time/fees/taxes, facility/system index modeling, invention/decryptors, SVR/price trend, queue/shopping list integration.

**Market pricing**
- Legacy uses multiple providers and/or ESI strategies to get prices.
- Web currently covers: cached prices + manual sync UI + batch region-order fetch.
- Web still needs (optional): alternate price providers (EVEMarketer/Fuzzworks) if we want fallback sources, and/or a strategy for buy-order pricing.

**Mining / Reprocessing**
- Legacy has dedicated reprocessing/mining workflows (e.g., reprocessing plant UI, ore conversion).
- Web: not started.

**Shopping lists / Planning**
- Legacy has shopping list building, exports, and asset-aware views.
- Web: not started.

## Source-of-Truth Docs
- `tasks.md`: detailed phase checklist and “what’s next” items.
- `plan.md`: high-level replacement plan and parity milestones.
- This file: legacy scope reference plus checkpoint mapping.
