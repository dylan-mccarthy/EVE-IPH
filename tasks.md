# Modernization Tasks (Backend + React SPA)

## Current Status (December 12, 2025)

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

### ✅ Phase 3 Complete: ESI Data Integration & Display

#### Completed Tasks
1. ✅ **Character Sync Endpoint** - POST /api/characters/{id}/sync refreshes ESI data on demand
   - Syncs skills, assets, industry jobs, market orders, wallet transactions
   - Returns statistics for each data type synced
   - Handles errors gracefully per data source
   - CharacterSyncService orchestrates all sync operations

2. ✅ **Assets Page** - Display character assets grouped by location with type names
   - Grouped by location with item counts
   - Search by type ID or name
   - Filter by location dropdown
   - Blueprint type badges (BPO/BPC/N/A)
   - Responsive table design
   - **SDE Integration**: Item icons, names, and groups displayed
   - Location name resolution from database

3. ✅ **Industry Jobs Page** - Show active/completed jobs with blueprints, costs, dates
   - Active/completed job filtering
   - Status badges (Active, Ready, Paused, Cancelled)
   - Activity type display (Manufacturing, Invention, etc.)
   - Duration formatting and cost display
   - Toggle to include/exclude completed jobs
   - **SDE Integration**: Blueprint names with icons, activity names from database, facility location names

4. ✅ **Market Orders Page** - Display active buy/sell orders with prices and volumes
   - Buy/Sell order filtering
   - Total value calculations
   - Volume remaining vs. total display
   - Price formatting with ISK abbreviations
   - **SDE Integration**: Item names with icons, location names from database

5. ✅ **Wallet Transactions Page** - Detailed transaction history with filters
   - Purchase/Sale filtering
   - Search by type ID or transaction ID (now searches by item name)
   - Summary cards for spent, earned, and net profit
   - Color-coded positive/negative values
   - Transaction type badges
   - **SDE Integration**: Item names with icons, location names from database

6. ✅ **SDE Service Implementation** - Static Data Export integration for type/location name resolution
   - **SDEService** with batch type lookup (prevents N+1 queries)
   - GetTypeInfoAsync/GetTypeInfoBatchAsync for item type info (name, group, category)
   - GetLocationNameAsync for station/structure names (checks STATIONS then INVENTORY_TYPES)
   - GetActivityNameAsync for industry activity names
   - REST endpoints: GET /api/sde/types/{id}, POST /api/sde/types/batch, GET /api/sde/locations/{id}, GET /api/sde/activities/{id}
   - Frontend integration with TypeScript types and batch fetching
   - Consistent icon styling across all pages

#### Services & Infrastructure Added
- **IMarketOrdersService**: Market orders from ESI with proper typing
- **MarketOrdersService**: Implementation with token refresh and error handling
- **ICharacterSyncService**: Interface for sync operations
- **CharacterSyncService**: Orchestrates all ESI data syncing
- **CharacterPersistenceService**: Extended with SaveSkillsAsync for skill persistence
- **ISDEService**: Interface for Static Data Export lookups
- **SDEService**: Implementation with batch queries following legacy VB patterns
- **SDEEndpoints**: REST endpoints for type info, locations, and activities
- **API Utility**: Centralized TypeScript API client with typed responses
- **Navigation**: Updated with links to all new pages (Assets, Industry, Market, Wallet)

### 🚧 Phase 4: Manufacturing Calculator (Current Priority)

The manufacturing calculator is the core feature of EVE-IPH. It calculates material costs, build times, profits, and ISK-per-hour (IPH) for manufacturing items in EVE Online.

#### Phase 4.1: Blueprint Data Service ✅ COMPLETE

1. ✅ **Blueprint Models** - Created comprehensive models for blueprint data
   - BlueprintDetails: Main blueprint with activities list
   - BlueprintActivity: Activity with materials and products
   - BlueprintMaterial: Material requirements with quantity, volume, consume flag
   - BlueprintProduct: Output products with probability

2. ✅ **Blueprint Service** - Database queries for blueprint information
   - BlueprintService.GetDetailsAsync: Queries ALL_BLUEPRINTS, ALL_BLUEPRINT_MATERIALS, INDUSTRY_ACTIVITY_PRODUCTS
   - GetActivityDetailsAsync helper for materials and products per activity
   - GetActivityName helper mapping activity IDs (1=Manufacturing, 3=TE, 4=ME, 5=Copying, 8=Invention, 11=Reactions)
   - Handles null blueprints gracefully

3. ✅ **Blueprint Endpoints** - REST API for blueprint access
   - GET /api/blueprints/search: Search blueprints with optional query, group, category, pagination
   - GET /api/blueprints/{blueprintId}: Get detailed blueprint info with all activities
   - Fixed BlueprintSearchRequest to make Query optional (was causing Bad Request errors)

4. ✅ **Blueprint UI** - Interactive manufacturing calculator interface
   - **Live Search Dropdown**: Search as you type with 300ms debounce, shows top 15 results
   - **Blueprint Selection**: Click from dropdown to load full details
   - **Manufacturing Controls Panel**:
     - ME (Material Efficiency) 0-10: Each level reduces materials by 1%
     - TE (Time Efficiency) 0-20: Ready for time calculations
     - Runs per BP, Number of BPs, Production Lines
     - Total Units (highlighted) - multiplies material quantities
      - **Cost Summary Cards**: Component Material Cost, Raw Material Cost, Market Price, Profit (server-calculated from cached market prices)
   - **Two-Column Materials Layout**:
     - Component Materials (left): Direct manufacturing inputs
          - Raw Materials (right): Recursive breakdown (raw expansion)
     - Both showing Material, Qty (adjusted for ME + Total Units), Cost/Item, Total Cost
    - **Server-side Quantity Calculations**: material quantities computed by backend (legacy-style rounding parity)
   - **Collapsible Other Activities**: Invention, Research, etc. hidden by default
   - **CSS Styling**: Compact tables, gradient headers, responsive grid layout

5. ✅ **Frontend Integration** - TypeScript types and API client
   - BlueprintSearchResponse, BlueprintSummary interfaces matching backend
   - BlueprintDetails, BlueprintActivity, BlueprintMaterial, BlueprintProduct interfaces
   - api.blueprints.search() and api.blueprints.getDetails() methods
   - Fixed property name mismatches (camelCase from JSON serialization)
   - Added null safety checks for selectedBlueprint rendering

#### Phase 4.2: Market Price Integration ✅ COMPLETE (OPTIMIZED)

**Goal**: Fetch and cache market prices from ESI to calculate material costs

##### Implemented Components

1. ✅ **MarketPrice Models** - Type-safe price data models
   - MarketPrice: TypeId, RegionId, BuyPrice, SellPrice, Volume, LastUpdated, ExpiresAt
   - MarketPriceRequest: TypeIds[], RegionId
   - MarketPriceResponse: Dictionary<int, MarketPrice>
   - RefreshPricesResponse: Updated, Failed counts

2. ✅ **ItemGroup Models** - Item group selection for bulk updates
   - ItemGroup: GroupId, GroupName, CategoryId, ItemCount
   - ItemGroupsResponse: List<ItemGroup>
   - ItemsByGroupRequest: GroupIds[], RegionId
   - ItemsByGroupResponse: TotalItems, TypeIds[]

3. ✅ **MarketPriceService** - ESI market data fetching with **BATCH OPTIMIZATION**
   - Database: MARKET_PRICES table with expiry index for performance
   - Cache: 1-hour expiry (3600 seconds), check ExpiresAt before returning stale data
   - **OPTIMIZED APPROACH**: Batch ESI requests instead of per-type
     - FetchAllOrdersForRegion: Single paginated request series to ESI `/markets/{region}/orders/?order_type=all&page={page}`
     - Reads X-Pages header from response, fetches all pages sequentially (max 100 for safety)
     - Returns List<EsiMarketOrder> with all region orders in memory
   - RefreshPricesAsync: Groups orders by TypeId locally, calculates buy/sell prices, saves to cache
   - Price Calculation: BuyPrice = Max(buy orders), SellPrice = Min(sell orders)
   - **Performance**: ~36 seconds for 11 items (all minerals) vs. N individual requests
   - **Note**: ESI batch endpoint only returns sell orders (market supply), not buy orders
     - BuyPrice will be 0.0 for most items (this is expected ESI behavior)
     - SellPrice is what matters for manufacturing cost calculations anyway

4. ✅ **Market REST Endpoints** - Complete API for price management
   - GET /api/market/prices/cached?typeIds=34,35&regionId=10000002 - Get cached prices
   - GET /api/market/prices/cached/{typeId}?regionId=10000002 - Single price lookup
   - POST /api/market/prices/refresh body:{typeIds[], regionId} - Force ESI refresh with batch optimization
   - GET /api/market/groups - Returns all 812 tradeable item groups with counts
   - POST /api/market/groups/items body:{groupIds[], regionId} - Get type IDs for selected groups

5. ✅ **MarketData Page** - Dedicated market price management UI
   - Two-column layout: Groups selection (left), Results table (right)
   - Checkbox selection for 812 tradeable item groups
   - Search filter across group names
   - Quick select buttons: Minerals, Ice, Moon Materials, Salvage
   - Fetch Prices workflow:
     - Get type IDs from selected groups
     - Refresh prices from ESI (batch optimized)
     - Display results with buy/sell prices, spread, success/fail status
   - Results table: Type ID, Item Name, Buy Price, Sell Price, Spread, Status
   - Summary cards: Total selected, success count, fail count
   - Complete CSS styling matching legacy app layout

6. ✅ **Frontend Integration** - TypeScript types and API methods
   - MarketPrice, ItemGroup interfaces in api.ts
   - api.market.getPrices(), refreshPrices(), getItemGroups(), getItemsByGroups()
   - Navigation link to /market-data page
   - ItemGroup defined locally in MarketData.tsx (not exported from api.ts)

7. ✅ **End-to-End Testing** - Verified with curl commands
   - GET /api/market/groups: 812 groups ✓
   - POST /api/market/groups/items (Minerals group 18): 11 type IDs ✓
   - POST /api/market/prices/refresh (3 types): updated:3, failed:0 ✓
   - GET /api/market/prices/cached: Real Jita prices cached correctly ✓
   - Price data: Tritanium 3.94 ISK, Pyerite 23.8 ISK, Mexallon 53.95 ISK ✓

##### ESI Integration Notes
- Region: The Forge (ID: 10000002) - Jita 4-4 trade hub
- ESI Endpoint: GET /markets/{region}/orders/?order_type=all&page={page}
- Pagination: X-Pages header indicates total pages, fetch all up to 100 max
- **Buy Orders Limitation**: Batch endpoint doesn't include buy orders (ESI design)
  - BuyPrice will be 0.0 in cache (expected behavior)
  - SellPrice is accurate and sufficient for manufacturing costs
  - To get buy orders, would need individual type-specific requests (defeats batch optimization)
- Rate Limiting: No longer needed with batch approach (single request series vs. concurrent)

##### Database Schema
```sql
CREATE TABLE IF NOT EXISTS MARKET_PRICES (
    TYPE_ID INTEGER PRIMARY KEY,
    REGION_ID INTEGER NOT NULL,
    BUY_PRICE REAL NOT NULL,
    SELL_PRICE REAL NOT NULL,
    VOLUME INTEGER NOT NULL,
    LAST_UPDATED TEXT NOT NULL,
    EXPIRES_AT TEXT NOT NULL
);
CREATE INDEX idx_market_prices_expires ON MARKET_PRICES(EXPIRES_AT);
```

##### Phase 4.3: Blueprint Manufacturing Cost Integration 🚧 IN PROGRESS

**Goal**: Keep the backend as the source of truth for manufacturing calculations (materials/time/fees/profit) and port legacy parity items incrementally with tests.

**Checkpoint (December 12, 2025) — Implemented**
- `POST /api/manufacturing/calculate` is live and the Blueprints calculator uses it (UI is a thin client)
- Market pricing reads used by the calculator are cache-only (no ESI refresh on read)
- `ManufacturingRequest` / `ManufacturingResponse` expanded to support calculator inputs + detailed breakdowns
- Manufacturing response includes both component materials and recursive raw-material breakdown
- Legacy-style material quantity rounding parity implemented server-side (shared helper) + focused unit tests
- CS1591 (missing XML comments) warnings suppressed to reduce build noise

**Next Iterations (Parity Increments)**

1. ✅ **Make test runs actionable (reduce unrelated noise)**
   - Fixed CharacterPersistenceService tests (SQLite in-memory schema persistence)
   - Aligned AssetsService “no token” test with current contract (service throws; endpoint maps to 401)
   - `dotnet test server.Tests` is green again

2. ✅ **Facility modifiers (materials + time)**
   - Added explicit request inputs for `FacilityMaterialMultiplier` and `FacilityTimeMultiplier`
   - Applied facility material multiplier to the legacy-style material rounding path

3. ✅ **Time + IPH (server-side)**
   - Exposed `BASE_PRODUCTION_TIME` via blueprint details and used it to compute time
   - IPH is derived from profit divided by wall-clock time (adjusted by TE, facility time multiplier, and production lines)

4. ✅ **Fees/taxes (server-side placeholders)**
   - Added request inputs: `SalesTaxRate`, `BrokerFeeRate`, `JobInstallationCost` (defaults are 0)
   - Profit calculation subtracts these values; pricing source rules unchanged

5. ✅ **Validation (small golden cases)**
   - Added manufacturing “golden” tests using mocked blueprints/prices to validate full response + IPH math

6. ✅ **Legacy-aligned market-mode pricing + fee semantics (product-side)**
   - Added `ProductMarketMode` to mirror legacy modes: Buy / Sell Order / Buy Order / Sell
   - Product value now uses min sell vs max buy based on mode; tax/broker are applied consistently with the selected mode
   - Defaults preserve current behavior (Sell Order + explicit rates)

7. ✅ **Legacy-aligned market-mode pricing + fee semantics (material-side)**
   - Added `MaterialMarketMode` to mirror legacy material acquisition: Buy (min sell) vs Buy Order (max buy + broker)
   - Component material unit prices now follow the selected mode; Buy Order includes broker fee as an acquisition cost
   - Raw-material breakdown pricing now follows the same mode (so totals stay consistent)

8. ✅ **Dual profit/IPH parity (components vs raw cost basis)**
   - Added `ProfitCostBasis` request knob (Components vs Raw) and server-side selection of Profit/IPH
   - Response now includes `ProfitComponents`, `ProfitRaw`, `IphComponents`, and `IphRaw` for easy parity validation
   - UI adds a selector for the basis and displays both variants alongside the selected Profit/IPH

9. ✅ **Build vs Buy components + excess sellback (opt-in)**
   - Added `ProfitCostBasis=BuildBuy` for an auto build-vs-buy calculation (cheapest per component)
   - Added `SellExcessItems` to allow net sellback of excess intermediate output (sell price minus sales tax + broker)
   - Response exposes `BuildBuyTotalCost` and `ExcessSellValueNet` when BuildBuy is selected
   - Added a focused golden test to lock behavior

**Remaining Phase 4.3 work (next up)**
- ✅ **System/job cost modeling (beyond placeholders)**
   - Added opt-in request inputs for system index + job fee modeling (legacy-shaped parity increment)
   - Uses adjusted-price EIV (`ITEM_PRICES_FACT.ADJUSTED_PRICE`) with a simple formula: `EIV * (systemIndex * costMultiplier + facilityTax + sccSurcharge)`
- ✅ **UI wiring for new inputs**
   - Surfaced facility multipliers + taxes/fees inputs on the Manufacturing Calculator
   - Requests sent to `/api/manufacturing/calculate` now include these values
   - IPH is displayed in the summary (server-side time/IPH is now visible)
---

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
