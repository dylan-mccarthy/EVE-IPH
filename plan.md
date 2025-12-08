# Modernization Plan – .NET 9 + Web Frontend

## Goals
- Move the WinForms application from .NET Framework 4.6.1 to a supported, modern stack targeting .NET 9 (Windows). 
- Preserve domain functionality (industry planning, pricing, mining, assets) while improving maintainability, reliability, and security.
- Reduce global coupling, adopt SDK-style builds, modernize HTTP/ESI flows, and enable CI/testing.

## Guiding Approach
- **Incremental, low-risk:** First convert to SDK-style and run on .NET 8 LTS (for tooling stability), then move to .NET 9 once dependencies and runtime are validated.
- **Dual-track UI:** Keep WinForms running during migration for parity, but add a React SPA front-end that can run in-browser (or packaged via Electron/PWA) backed by a .NET 9 API. VB→C# remains optional if tooling gaps appear.
- **Windows-only backend:** Target `net9.0-windows` and x64; drop legacy Itanium/x86 unless explicit need arises.
- **PackageReference-only:** Remove `packages.config` and fold NuGet management into the project file.

## Workstream Outline
1. **Assessment & Baseline**
   - Capture current build outputs and key user flows; identify critical paths in `frmMain`, ESI auth, DB access, and pricing.
   - Freeze update channels to avoid runtime auto-updates during migration.
      - Identify UI views to mirror first in the web client (e.g., blueprint list, manufacturing calc, shopping list, market prices).

2. **Project Conversion**
   - Convert `EVE Isk per Hour.vbproj` to SDK-style (`UseWindowsForms`, `EnableVisualStyles`, icon/manifest wiring).
   - Migrate NuGet to `PackageReference`; upgrade to latest compatible versions: `Newtonsoft.Json`, SQLite provider (consider `System.Data.SQLite.Core` or `Microsoft.Data.Sqlite`), `LpSolveDotNet` alternative if needed.
   - Standardize output paths (`bin/Debug|Release`), remove Itanium configs, prefer x64/AnyCPU.
      - Consider splitting backend into a class library + ASP.NET Core Web API host for the React app.

3. **Target Framework Upgrade**
   - Step to `net8.0-windows` and fix compilation issues (namespaces, API replacements, designer resources).
   - Resolve WinForms designer artifacts and resource loading; validate all forms open.
   - Move to `net9.0-windows` after dependency verification.
      - Expose calculation/ESI/DB access through Web API endpoints (ASP.NET Core minimal APIs or controllers) for React consumption.

4. **Runtime Modernization**
   - Replace `HttpWebRequest/WebClient` with `HttpClient` + `async/await`; centralize ESI client with resilient retries and rate-limit handling.
   - Replace global mutable state with injectable services (settings, DB, ESI, price providers); introduce a lightweight composition root (simple DI container or factory module).
      - Define a Web API surface (auth, settings, blueprints, manufacturing calc, mining, shopping list, assets, market data) consumed by the React app; apply auth/CSRF/caching policies.
   - Encapsulate SQLite access behind repository/service classes; consider `Microsoft.Data.Sqlite` with WAL; ensure thread-safe usage without global locks where possible.
      - Evaluate exposing data via gRPC/REST for the SPA; start with REST + JSON.
   - Move configuration to `appsettings.json`-style, minimize hard-coded URLs; secure client secrets (if any) via Windows credential storage.

5. **Domain & UI Refactoring**
   - Break down `frmMain` into feature-specific controllers/services; reduce code-behind size by extracting domain logic.
      - For the web client, build feature slices (e.g., Blueprint search, Manufacturing calculator, Price updater) backed by shared domain services in the backend.
   - Normalize models (blueprints, facilities, prices, shopping items) into dedicated classes with unit-tested logic.
   - Introduce background worker/Task-based patterns for long-running calculations; improve cancellation tokens vs. flag booleans.
   - Refresh UX where valuable (responsive layouts, consistent theming) while preserving workflows.

6. **Testing & Quality**
   - Add unit tests for calculation-heavy modules (industry yield, tax/fee math, shopping list aggregation).
      - Add API contract tests (backend) and component tests (React) for critical flows.
   - Add integration tests for ESI/market providers with recorded fixtures.
   - Wire CI (GitHub Actions) for build + tests on `net8.0-windows`/`net9.0-windows` matrix.

7. **Deployment & Updates**
   - Replace custom updater with MSIX or ClickOnce for Windows, or modernize existing updater to use HTTPS, signatures, and version manifest checks.
      - For the SPA, plan static hosting (e.g., GitHub Pages, S3/CloudFront) or bundle into Electron if offline support is required.
   - Produce signed installers; document migration steps for existing users (DB location, settings carry-over).

8. **Data Migration & Compatibility**
   - Validate SQLite schema compatibility; script migrations if provider changes.
      - Provide API endpoints for settings/DB migration and enforce version negotiation between SPA and backend.
   - Ensure user settings migration from legacy storage to new format (JSON); implement import/backfill.

## Risks / Decisions to Track
- VB.NET tooling and WinForms designer stability on .NET 9; contingency to convert to C# if blocking.
- `LpSolveDotNet` availability for `net9.0-windows`; may require swapping to another ILP/LP library or native interop.
- Auto-update channel: consider disabling during rollout to avoid mixed-runtime installs.

## Success Criteria
- Project builds and runs on `net9.0-windows` x64 with feature parity for core workflows.
- CI green on build/test; packaged installer produced from pipeline.
- Reduced crash/log volume; measurable decrease in global-state usage (services extracted, tests cover core calc logic).
