# Modernization Tasks (Backend + React SPA)

## Current Status (December 9, 2025)

### ✅ Phase 1 Complete: Authentication & Token Management
- **ASP.NET Core 9 Backend**: Minimal API server running on port 5056
- **React + Vite Frontend**: Running on port 5173 with TypeScript, React Router
- **EVE SSO OAuth2**: PKCE flow with SHA256, state management, 17 scopes configured
- **Authentication Flow**: Login → callback handling → token storage → error handling
- **Token Persistence**: Tokens stored in ESI_CHARACTER_DATA table
- **Token Refresh**: Automatic refresh 5 minutes before expiry, transparent to frontend
- **Token Management Services**: TokenStore, TokenRefreshService with proper error handling
- **Skills Display**: Character skills page with ESI integration, database enrichment, grouped display
- **Database Integration**: SQLite connection via Microsoft.Data.Sqlite (ReadWriteCreate mode)
- **Error Handling**: Structured ApiError responses, graceful auth error recovery
- **CORS Configuration**: Frontend/backend communication working

### ✅ Phase 2 Complete: Character Management & Core UI
- **Character List Endpoint**: GET /api/characters with all character data
- **Character Selection UI**: Character selector page with grid layout, portraits, default badges
- **Character Switching**: Seamless switching between characters with localStorage persistence
- **Default Character Support**: Auto-select default character on login, visual badges
- **Character Profile Page**: Detailed profile with skills, wallet, corporation, security status, scopes
- **Navigation Component**: Top navigation bar with login/logout, character display, route links
- **Page Structure**: Consistent navigation across Characters/Skills/Profile pages
- **Skills Page**: Skills grouped by category, total SP displayed, character-specific view
- **Configuration**: Dual config setup (appsettings.json + appsettings.Development.json)

### ✅ ESI Data Services Implemented
- **WalletService**: Balance, transactions, journal endpoints with ESI integration
- **AssetsService**: Character assets with pagination support
- **IndustryService**: Industry jobs (active/completed) from ESI
- **Character Enrichment**: Wallet balance displayed on profile page
- **Scopes Configuration**: 17 scopes including wallet, assets, industry, market orders, corporation data

### 🚧 Phase 3: ESI Data Integration & Display (Current Focus)

#### Immediate Next Tasks
1. **Character Sync Endpoint** - POST /api/characters/{id}/sync to refresh ESI data on demand
2. **Assets Page** - Display character assets grouped by location with type names
3. **Industry Jobs Page** - Show active/completed jobs with blueprints, costs, dates
4. **Market Orders Page** - Display active buy/sell orders with prices and volumes
5. **Wallet Transactions Page** - Detailed transaction history with filters

#### Blueprint Data Integration
6. **Blueprints Endpoint** - GET /api/characters/{id}/blueprints with ME/TE/runs from ESI
7. **Blueprint List Page** - Frontend display of owned blueprints with filters
8. **Blueprint Search** - Search and filter blueprints by type, ME/TE levels
9. **Blueprint Management** - Update blueprint data, mark favorites

#### Corporation Data
10. **Corporation Assets** - GET /api/corporations/{id}/assets (for CEO/Director roles)
11. **Corporation Wallet** - Corporation wallet balance and divisions
12. **Corporation Blueprints** - Corporation-owned blueprints access

### 📋 Phase 4: Manufacturing & Pricing System (Planned)

#### Blueprint & Manufacturing Calculations
- Blueprint search and filtering across character/corporation blueprints
- Material requirements calculation from INVENTION_MATERIALS and related tables
- Manufacturing cost calculator (materials + facility fees + taxes)
- Apply skill bonuses (ME, TE, industry skills) from character skills
- Facility modifiers (structure bonuses, rigs, system bonuses)
- Profit/IPH calculations with build time and costs
- Component building (recursive calculation for T2/T3)
- Invention calculations for T2 blueprints

#### Market Data Integration
- ESI market data endpoint integration for real-time prices
- EVEMarketer API integration as fallback pricing source
- Region/system scoping for accurate pricing
- Price caching strategy (in-memory with TTL, consider Redis for distributed)
- Historical price data for trend analysis
- Buy/sell price selection (use buy orders for selling, sell orders for buying materials)

### 📋 Phase 5: Mining & Resources (Planned)
- Ore/ice calculator with volume and value
- Reprocessing calculations with skills/station bonuses
- Moon mining profitability calculator
- Gas harvesting calculations
- Planetary Interaction (PI) setup and profit calculations

### 📋 Phase 6: Shopping List & Planning (Planned)
- Multi-item manufacturing planning
- Shopping list aggregation across multiple builds
- Buy order placement planning and tracking
- Hauling optimization (volume, collateral, routes)
- Production queue management

## Technical Debt & Improvements

### ✅ Completed Architecture Improvements
- **Service Layer**: Clean separation between endpoints, services, and data access
- **Error Handling**: Consistent ApiError responses across all endpoints
- **Logging**: Structured logging with ILogger throughout services
- **Token Security**: Database-persisted, auto-refresh 5min before expiry
- **Database Permissions**: ReadWriteCreate mode for SQLite operations
- **React State Management**: localStorage session persistence, no stale closures
- **Navigation System**: Consistent UI with Navigation component across all pages
- **Configuration Management**: Dual config support (Development overrides Production)

### Next Architecture Improvements
- **Caching Layer**: Add in-memory caching for market data, static game data (consider Redis for distributed)
- **Rate Limiting**: ESI rate limit handling (100 requests/10s with error backoff)
- **Background Jobs**: Hangfire or Quartz.NET for periodic updates (market prices, industry jobs, blueprints)
- **Request Validation**: FluentValidation for endpoint inputs and query parameters
- **API Versioning**: Add version prefix (/api/v1/) for future compatibility
- **ESI Error Handling**: Improved retry logic with exponential backoff
- **Data Synchronization**: Batch sync endpoints for multiple characters
- **Real-time Updates**: SignalR for live market price updates, job completion notifications

### Testing Strategy
- Unit tests for calculation logic (manufacturing costs, reprocessing yields, ME/TE bonuses)
- Integration tests for ESI data fetching with mock responses
- API contract tests for endpoint consistency
- Frontend component tests (React Testing Library for character selection, profile, skills)
- E2E tests for critical flows (Playwright: login → character select → skills view)
- Performance tests for large blueprint calculations

### Known Issues to Address
- **Scope Re-authentication**: Users need to logout/login after scope updates in config
- **ESI Error Messages**: Need better user-facing error messages for ESI failures
- **Token Expiry Edge Cases**: Handle token revocation during active requests
- **Large Asset Lists**: Pagination UI for characters with 10,000+ assets
- **Blueprint Sorting**: Need efficient sorting for large blueprint collections

## Implementation Notes & Lessons Learned

### Configuration Management
- **appsettings.json vs appsettings.Development.json**: Development config takes precedence in dev environment
- **Scope Updates**: Both config files must be updated when changing OAuth scopes
- **Re-authentication Required**: Users must logout/login to receive updated scopes

### Database Operations
- **SQLite Connection Mode**: Use ReadWriteCreate for all database operations
- **Connection String**: Important to specify mode explicitly in connection string
- **Transaction Management**: Consider connection pooling for high-frequency operations

### React State Management
- **localStorage Persistence**: Essential for session management across page refreshes
- **Stale Closures**: Use functional updates or proper dependency arrays in useEffect
- **Key Props**: Always provide unique keys for list items to avoid React warnings

### ESI Integration
- **Token Management**: Auto-refresh 5 minutes before expiry to avoid interruption
- **Scope Checking**: Always verify character has required scope before ESI calls
- **Pagination**: Handle ESI pagination with X-Pages header for large datasets
- **Error Handling**: ESI can return 502/504, implement retry logic

### UI/UX Best Practices
- **Navigation Component**: Centralized navigation improves consistency and maintainability
- **Active Route Highlighting**: Visual feedback improves user orientation
- **Responsive Design**: Mobile-first approach with breakpoints at 768px and 480px
- **Loading States**: Always show loading indicators during async operations

## Server Implementation Breakdown (Detailed)
- ✅ Token refresh service: Auto-refresh 5min before expiry, handle revocation
- ✅ Character token persistence: Save/load from ESI_CHARACTER_DATA table
- ✅ Wallet/Assets/Industry services: Pull from ESI with pagination and error handling
- 🚧 Character sync endpoint: Refresh character data on demand from ESI
- 🚧 Manufacturing service: Material requirements, skill/facility modifiers, cost/profit/IPH
- 🚧 Market service: Price providers (ESI/EVEMarketer), region/system scoping, caching
- 🚧 Settings persistence: Server-side JSON storage with environment overrides
- 🚧 API validation: FluentValidation for all endpoint inputs
- 🚧 Testing suite: Unit tests for calculations, integration tests for ESI calls
- 🚧 Observability: Request logging, correlation IDs, performance metrics

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
