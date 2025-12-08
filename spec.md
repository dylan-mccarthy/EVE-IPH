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
