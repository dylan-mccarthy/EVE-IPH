# Modernization Tasks (Backend + React SPA)

## 0) Prep & Baseline
- Document critical user flows and gather current binaries for reference.
- Freeze auto-updates during migration; capture current settings/DB locations.

## 1) Project Conversion
- Convert `EVE Isk per Hour.vbproj` to SDK-style (`UseWindowsForms`, icon/manifest wiring).
- Migrate `packages.config` to `PackageReference`; remove `packages/` folder reliance.
- Normalize configurations: drop Itanium, prefer x64/AnyCPU; fix output paths.
 - Split out a backend class library (domain/services) and an ASP.NET Core Web API host to serve the React app.

## 2) Framework Upgrade
- Retarget to `net8.0-windows` first; resolve compile errors (namespaces, designer resources).
- Validate WinForms designers; fix resx/designer mismatches.
- Retarget to `net9.0-windows` after dependencies verified.
 - Bring up ASP.NET Core host on `net8.0-windows` → `net9.0-windows`; wire minimal APIs for early endpoints.

## 3) Dependency Updates
- Update `Newtonsoft.Json` to latest; evaluate moving to `System.Text.Json` where feasible.
- Replace `Stub.System.Data.SQLite.Core.NetFramework` with `System.Data.SQLite.Core` (or `Microsoft.Data.Sqlite` if viable); adjust code for provider API differences.
- Confirm `LpSolveDotNet` compatibility; identify alternative LP solver if needed.
 - Add ASP.NET Core packages (`Microsoft.AspNetCore.App`), authentication middleware, and CORS configuration.
 - Choose React stack: Vite + TypeScript + React Query + Zustand/Redux (lightweight), component library (e.g., MUI or Mantine).

## 4) Architecture/Code Health
- Introduce composition root and services for settings, DB, ESI, pricing, calculations; reduce `Globals.vb` reliance.
- Refactor `frmMain` by extracting feature controllers/services (manufacturing, mining, pricing, assets, shopping list).
- Replace `Thread`/flags with `async/await`, `Task`, and `CancellationToken` for long operations.
- Centralize HTTP with `HttpClientFactory`-style helper; add resilient retry/backoff and rate-limit handling for ESI/market calls.
- Wrap SQLite access with repositories; ensure WAL, parameterized queries, and proper disposal.
 - Define Web API contracts (auth/session, settings, blueprints, manufacturing calc, mining, shopping list, assets, market prices); version the API.
 - Add DTOs and mappers between domain models and API payloads; keep UI-agnostic domain logic in shared library.

## 5) Testing & CI
- Add unit tests for calculation modules (industry yields, taxes/fees, shopping aggregation).
- Add integration tests with recorded fixtures for ESI/market data.
- Set up GitHub Actions CI to build/test on Windows for `net8.0-windows` and `net9.0-windows`.
 - Add API contract tests (backend) and component/E2E tests (React via Playwright/Cypress).
 - Lint/format pipelines for frontend (ESLint/Prettier) and backend (StyleCop/FXCop analyzers).

## 6) Configuration & Settings
- Migrate settings to JSON (`appsettings.json` + user scope) and backfill from legacy settings files.
- Externalize URLs (ESI, update manifests, market providers); avoid hard-coded strings where possible.
- Secure any secrets (client IDs/keys) via Windows credential APIs.
 - Add CORS, HTTPS, and auth/session configuration for the Web API; store tokens securely (http-only cookies or local secure storage).

## 7) Updater / Deployment
- Decide on deployment: MSIX or modernized ClickOnce; deprecate legacy updater EXEs if possible.
- Implement signed build artifacts; update versioning strategy.
- Provide migration guide for users (DB path, settings import, re-auth if scopes change).
 - For the SPA: decide hosting (static site + API), or bundle via Electron/PWA for offline; set up CI build to produce static assets.

## 8) UX/Performance Tweaks (optional)
- Improve responsiveness for grid-heavy tabs; move heavy calc to background tasks with progress/cancel.
- Consider modest UI refresh (consistent theming, DPI awareness).
 - Design responsive React UI: key pages (Dashboard, Blueprint search, Manufacturing calc, Market prices, Shopping list, Mining). Add optimistic updates and caching via React Query.

## 9) Data & Migration
- Validate SQLite schema compatibility; write migration scripts if provider changes.
- Ensure settings/DB are not overwritten during updates; preserve user data.
 - Add API endpoints to migrate/import legacy settings and cached data for the SPA.

## 10) Tracking & Rollout
- Create milestone issues for each workstream; prioritize blockers (project conversion, dependencies, ESI client).
- Establish release checkpoints: beta on net8, GA on net9 after validation.
 - Plan phased SPA rollout: alpha (internal), beta (power users), GA after parity with WinForms core flows.
