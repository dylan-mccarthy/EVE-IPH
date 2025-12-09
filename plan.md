# Modernization Plan – .NET 9 + Web Frontend (Backend-first)

## Goals
- Move the legacy WinForms app from .NET Framework 4.6.1 to a modern stack on .NET 9 (Windows).
- Stand up a browser-based React client backed by a .NET Web API while keeping WinForms operable during transition.
- Reduce global coupling, adopt SDK-style builds, modernize HTTP/ESI flows, and enable CI/testing.

## Guiding Approach
- **Backend-first:** Stand up the ASP.NET Core backend and shared domain library before deep React work; the new stack will fully replace WinForms (no need to keep it functional beyond reference).
- **Incremental runtime move:** Convert to SDK-style, target `net8.0-windows` first for tooling stability, then `net9.0-windows` once dependencies are validated.
- **Single UI target:** React SPA becomes the primary UI; VB→C# remains optional if tooling blocks.
- **Windows-only backend:** Target x64; drop Itanium/x86 unless required.
- **PackageReference-only:** Remove `packages.config` and fold NuGet into SDK projects.

## Workstreams (Backend first, then Frontend)

### Backend (priority)
1) **Assessment & Baseline**
   - Capture current build outputs and key user flows; map critical paths in `frmMain`, ESI auth, DB access, pricing.
   - Freeze auto-updates during migration; WinForms serves only as reference, not as a supported runtime target post-replacement.

2) **Project Conversion & Framework**
   - Convert legacy `.vbproj` to SDK-style; migrate to `PackageReference`; standardize x64 outputs.
   - Step to `net8.0-windows`; fix compile issues; then move to `net9.0-windows`.
   - Stand up ASP.NET Core Web API host (minimal APIs/controllers) plus shared domain library.

3) **Runtime Modernization**
   - Replace `WebClient/HttpWebRequest` with `HttpClient` + `async/await`; centralize ESI client with retry/rate-limit handling.
   - Introduce DI; extract services for settings, DB, ESI, pricing, manufacturing, mining, shopping list.
   - Encapsulate SQLite via repositories (`Microsoft.Data.Sqlite` or `System.Data.SQLite`), WAL, parameterized queries.
   - Define API surface (auth/session, settings, blueprints, manufacturing calc, mining, shopping list, assets, market prices); version endpoints; add CORS/auth.

4) **Domain Refactor & Parity**
   - Break down `frmMain` logic into reusable domain services; normalize models (blueprints, facilities, prices, shopping items).
   - Replace flag-based threading with Task + CancellationToken for long-running calcs.
   - Aim for functional parity in the web/API stack; WinForms parity is not required once replacement is live.

5) **Testing & CI**
   - Unit tests for calculations; integration tests for ESI/market providers with fixtures.
   - API contract tests; GitHub Actions CI for build/test on `net8/9` Windows.

6) **Deployment**
   - Choose packaging (MSIX/ClickOnce or MSIX + API host); sign artifacts; document settings/DB locations.
   - Provide migration endpoints for settings/data; enforce API version negotiation.

### Frontend (after backend basics are stable)
1) **Scaffold & Tooling**
   - Vite + React + TypeScript; lint/format/test (ESLint, Prettier, Vitest + RTL).
   - Base API client with `VITE_API_BASE`; CORS alignment with backend.

2) **Feature Slices**
   - Pages: Dashboard, Blueprint search, Manufacturing calculator, Market prices, Shopping list, Mining.
   - Shared components: layout shell, data tables, forms, toasts, loading/error states.

3) **Data & State**
   - React Query for server state; light client state via Zustand/Redux Toolkit if needed.
   - Type-safe DTOs matching backend contracts.

4) **Testing & Delivery**
   - Component tests (Vitest/RTL); E2E (Playwright/Cypress) for critical flows.
   - Build pipeline to emit static assets; hosting choice (static site + API) or Electron/PWA if offline required.

## Risks / Decisions to Track
- VB.NET tooling and WinForms designer stability on .NET 9; contingency to convert to C# if blocking.
- `LpSolveDotNet` availability for `net9.0-windows`; may require swapping to another ILP/LP library or native interop.
- Auto-update channel: consider disabling during rollout to avoid mixed-runtime installs.
- API/SPA version skew; enforce versioning and compatibility checks.

## Success Criteria
- Backend: API running on `net9.0-windows` x64 with parity for core calculations, CI green, signed artifacts.
- Frontend: React SPA consuming versioned API, passing component/E2E tests, deployable as static assets (or Electron/PWA if chosen).
- Reduced crash/log volume; measurable decrease in global-state usage via extracted services and tests covering core logic.
