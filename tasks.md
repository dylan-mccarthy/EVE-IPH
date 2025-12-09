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
   - **Cost Summary Cards**: Component Cost, Raw Cost, Market Price, Profit (all "TBD" pending prices)
   - **Two-Column Materials Layout**:
     - Component Materials (left): Direct manufacturing inputs
     - Raw Materials (right): Same as components for now (will break down components later)
     - Both showing Material, Qty (adjusted for ME + Total Units), Cost/Item, Total Cost
   - **Real-time ME Calculations**: `Math.ceil(baseQty * (1 - ME * 0.01) * totalUnits)`
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

##### Phase 4.3: Blueprint Manufacturing Cost Integration (NEXT)

**Goal**: Wire market prices into Blueprints.tsx manufacturing calculator for real cost calculations

#### Manufacturing Calculator Components

##### 1. Blueprint Search & Selection
- Search blueprints by name/type across owned blueprints (character + corporation if applicable)
- Filter by tech level (T1, T2, T3), category, group
- Display blueprint ME/TE levels, number of runs, location
- Show whether owned or need to calculate from market blueprint
- Quick access to recently used blueprints

##### 2. Material Requirements Calculator
**Backend Service: MaterialsCalculationService**
```csharp
// Calculate base materials from INDUSTRY_ACTIVITY_MATERIALS
// Apply ME (Material Efficiency) reduction: roundup(base_quantity * (1 - ME/100))
// Apply skill bonuses (Advanced Industry, specific industry skills)
// Handle recursive calculations for components (T2/T3 items)
// Track material tree depth to prevent circular dependencies
```

**Calculations:**
- Base materials from database
- ME reduction: `Math.Ceiling(baseQty * (1 - ME / 100.0))`
- Advanced Industry skill: 1% per level reduction
- Component manufacturing: recursive calculation with own blueprints

##### 3. Manufacturing Cost Calculator
**Backend Service: ManufacturingCostService**
```csharp
// Material costs from market prices (buy orders for materials)
// Facility fees = base_cost * system_cost_index * structure_bonus * tax_rate
// Manufacturing tax (default 10% at NPC stations)
// Installation cost based on estimated item value
```

**Cost Components:**
- Material costs (from market or custom prices)
- System cost index multiplier (from INDUSTRY_SYSTEMS_COST_INDICIES)
- Structure bonuses (Raitaru: -1%, Azbel: -2%, Sotiyo: -4%)
- Structure rigs (T1: -2.4%, T2: -2.1% per rig, max 3)
- Corporation tax rate
- Installation fees

##### 4. Build Time Calculator
**Backend Service: BuildTimeService**
```csharp
// Base time from INDUSTRY_ACTIVITY_MATERIALS
// Apply TE (Time Efficiency): base_time * (1 - TE/100)
// Apply skill bonuses:
//   - Industry skill: 4% per level
//   - Advanced Industry: 3% per level  
//   - Specific industry skills: varies per activity
// Apply structure bonuses (Raitaru/Azbel/Sotiyo, rigs)
// Apply implant bonuses
```

**Time Modifiers:**
- TE reduction: `baseTime * (1 - TE / 100.0)`
- Industry skill: 4% per level
- Advanced Industry: 3% per level
- Structure time bonus (Raitaru: -15%, Azbel: -20%, Sotiyo: -25%)
- Implants (various, from character data)

##### 5. Invention Calculator (for T2 blueprints)
**Backend Service: InventionService**
```csharp
// Base probability from INDUSTRY_ACTIVITY_PROBABILITIES
// Apply skill bonuses (Science skills: 1% per level each, encryption: 2% per level)
// Apply decryptor modifiers (probability multiplier, run/ME/TE modifiers)
// Calculate invention materials cost (datacores, data interfaces)
// Calculate average T2 BPC cost = invention_cost / (success_rate * runs_per_success)
```

**Invention Mechanics:**
- Base success probability from database
- Science skills (2 required): +1% per level each
- Encryption Methods: +2% per level
- Decryptors: modify probability, runs, ME, TE
- Calculate per-run invention cost for T2 items

##### 6. Profit & IPH Calculator
**Backend Service: ProfitCalculationService**
```csharp
// Revenue = sell_price * quantity * (1 - sales_tax - broker_fee)
// Profit = revenue - total_cost
// ISK per hour = profit / (build_time_seconds / 3600)
// ROI = (profit / total_cost) * 100
```

**Profit Metrics:**
- Total cost (materials + fees + taxes)
- Revenue (sell orders - broker fees - sales tax)
- Raw profit and profit margin %
- ISK per hour (profit / build time)
- Return on investment (ROI)

---

## Phase 4.2: Market Price Integration (IN PROGRESS)

**Goal**: Fetch and cache market prices from ESI to populate cost calculations in manufacturing calculator

### Implementation Plan

#### 1. Backend Market Price Service

**Create IMarketPriceService Interface**:
```csharp
public interface IMarketPriceService
{
    Task<MarketPrice?> GetPriceAsync(int typeId, int regionId = 10000002);
    Task<Dictionary<int, MarketPrice>> GetPricesAsync(IEnumerable<int> typeIds, int regionId = 10000002);
    Task RefreshPricesAsync(IEnumerable<int> typeIds, int regionId = 10000002);
}
```

**MarketPrice Model**:
```csharp
public record MarketPrice(
    int TypeId,
    int RegionId,
    decimal BuyPrice,      // Highest buy order
    decimal SellPrice,     // Lowest sell order
    long Volume,           // 24h volume
    DateTime LastUpdated,
    DateTime ExpiresAt
);
```

**ESI Endpoint**: `GET https://esi.evetech.net/latest/markets/{region_id}/orders/?type_id={type_id}`
- Default region: The Forge (Jita) - region ID 10000002
- Cache for 1 hour (respect ESI cache headers)
- Filter orders by is_buy_order true/false
- Calculate: highest buy = max(price where is_buy_order=true)
- Calculate: lowest sell = min(price where is_buy_order=false)

#### 2. Database Schema for Price Cache

```sql
CREATE TABLE MARKET_PRICES (
  TYPE_ID INTEGER PRIMARY KEY,
  REGION_ID INTEGER,
  BUY_PRICE REAL,
  SELL_PRICE REAL,
  VOLUME INTEGER,
  LAST_UPDATED TEXT,
  EXPIRES_AT TEXT
);

CREATE INDEX idx_market_prices_expires ON MARKET_PRICES(EXPIRES_AT);
```

#### 3. REST API Endpoints

- `POST /api/market/prices/refresh` - Force refresh prices for given type IDs
  - Request: `{ "typeIds": [34, 35, 36], "regionId": 10000002 }`
  - Response: `{ "updated": 3, "failed": 0 }`

- `GET /api/market/prices?typeIds=34,35,36&regionId=10000002` - Get cached prices
  - Response: `{ "prices": { "34": { buyPrice: 5.5, sellPrice: 6.0, ... }, ... } }`

- `GET /api/market/prices/{typeId}?regionId=10000002` - Single price lookup
  - Response: `{ "typeId": 34, "buyPrice": 5.5, "sellPrice": 6.0, ... }`

#### 4. Frontend Integration

**Update api.ts**:
```typescript
export interface MarketPrice {
  typeId: number;
  regionId: number;
  buyPrice: number;
  sellPrice: number;
  volume: number;
  lastUpdated: string;
  expiresAt: string;
}

export const market = {
  getPrices: async (typeIds: number[], regionId = 10000002) => 
    api.get<{prices: Record<number, MarketPrice>}>('/market/prices', { typeIds: typeIds.join(','), regionId }),
  
  refreshPrices: async (typeIds: number[], regionId = 10000002) =>
    api.post<{updated: number, failed: number}>('/market/prices/refresh', { typeIds, regionId })
};
```

**Update Blueprints.tsx**:
- Add `marketPrices` state: `useState<Record<number, MarketPrice>>({})`
- Fetch prices after blueprint loads: extract all material type IDs, call `market.getPrices()`
- Add "Update Prices" button with loading spinner
- Display prices in material tables: Cost/Item column shows sellPrice
- Calculate Total Cost: `(adjustedQty * sellPrice).toFixed(2)`
- Update cost summary cards with real totals

#### 5. Price Update Strategy

**Cache Expiration**: 1 hour (3600 seconds)
- Store `expiresAt = lastUpdated + 3600s`
- Check `expiresAt < DateTime.Now` before returning cached price
- If expired, fetch fresh data from ESI

**Rate Limiting**: ESI allows ~150 requests/second
- Batch requests: fetch all materials in single call per type
- Use concurrent requests with SemaphoreSlim(10) for throttling
- Cache aggressively to minimize ESI calls

**Error Handling**:
- If ESI fails, return last cached price (even if expired)
- Log errors but don't break calculator
- Show "Price unavailable" if no cache exists

---

#### Manufacturing Calculator UI (React)

**Main Calculator Page** (`/manufacturing`):
```
┌─────────────────────────────────────────────┐
│ Manufacturing Calculator                     │
├─────────────────────────────────────────────┤
│ Blueprint Search: [____________] [Search]    │
│ Tech Level: [All ▼] Category: [All ▼]       │
│                                              │
│ Selected: Raven Blueprint (ME: 10, TE: 20)  │
│ Runs: [1] Location: [Jita 4-4 ▼]           │
│                                              │
│ ┌─ Materials ──────────────────────────┐    │
│ │ Tritanium         1,234,567          │    │
│ │ Pyerite             456,789          │    │
│ │ ... (show/hide components tree)       │    │
│ └──────────────────────────────────────┘    │
│                                              │
│ ┌─ Costs & Profit ─────────────────────┐    │
│ │ Material Cost:     123.4M ISK        │    │
│ │ Installation:        5.2M ISK        │    │
│ │ Total Cost:        128.6M ISK        │    │
│ │                                       │    │
│ │ Sell Price:        145.0M ISK        │    │
│ │ Profit:             16.4M ISK        │    │
│ │ Margin:             12.7%            │    │
│ │ Build Time:         2h 34m           │    │
│ │ ISK/Hour:           6.4M             │    │
│ └──────────────────────────────────────┘    │
│                                              │
│ [Calculate] [Save to Queue] [Shopping List] │
└─────────────────────────────────────────────┘
```

#### Backend API Endpoints

```csharp
// Blueprint search
GET /api/blueprints/search?query={name}&techLevel={level}&category={cat}
  → Returns: BlueprintSearchResponse { items, total, page, pageSize }

// Get blueprint details
GET /api/blueprints/{blueprintTypeId}
  → Returns: BlueprintDetails { typeId, name, activities[], materials[], products[] }

// Calculate manufacturing
POST /api/manufacturing/calculate
  Body: {
    blueprintTypeId: number,
    runs: number,
    materialEfficiency: number,
    timeEfficiency: number,
    facilityType: string,
    systemId: number,
    characterId: number,
    customPrices?: { [typeId: number]: number }
  }
  → Returns: ManufacturingResult {
    materials: MaterialRequirement[],
    materialCost: number,
    installationCost: number,
    totalCost: number,
    buildTime: number,
    sellPrice: number,
    profit: number,
    profitMargin: number,
    iskPerHour: number
  }
```

#### Implementation Plan

**Phase 4.1: Blueprint Data Service**
1. ✅ Create BlueprintService with database queries for blueprint data
2. ⬜ Create endpoints for blueprint search and details
3. ⬜ Add blueprint list page in React (show owned blueprints)

**Phase 4.2: Material Calculations**
4. ⬜ Implement MaterialsCalculationService
   - Query INDUSTRY_ACTIVITY_MATERIALS
   - Apply ME reduction formula
   - Apply skill bonuses from character data
   - Handle recursive component calculations
5. ⬜ Create unit tests for material calculations

**Phase 4.3: Cost & Time Calculations**  
6. ⬜ Implement BuildTimeService (TE, skills, facility bonuses)
7. ⬜ Implement ManufacturingCostService (materials, fees, taxes)
8. ⬜ Implement market price integration (ESI market data)
9. ⬜ Create unit tests for cost/time calculations

**Phase 4.4: Manufacturing Calculator Orchestration**
10. ⬜ Create ManufacturingCalculatorService orchestrating all calculations
11. ⬜ Implement POST /api/manufacturing/calculate endpoint
12. ⬜ Add integration tests with realistic scenarios

**Phase 4.5: Frontend Manufacturing Calculator**
13. ⬜ Create Manufacturing page with blueprint search
14. ⬜ Add material list display with tree view for components
15. ⬜ Add cost breakdown and profit display
16. ⬜ Add facility selection and settings
17. ⬜ Connect to backend calculation endpoint

**Phase 4.6: Invention System (T2 Manufacturing)**
18. ⬜ Implement InventionService for T2 blueprint calculations
19. ⬜ Add invention cost to manufacturing calculator
20. ⬜ Add decryptor selection UI

#### Legacy VB Code References
For implementation guidance, refer to:
- `ManufacturingFacility.vb` - Facility bonuses and calculations
- `IndustryCalcFunctions.vb` - Core manufacturing math
- `frmManufacturingTab.vb` - UI layout and user flows
- `CalculateBlueprint()` functions - Main calculation orchestration

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
