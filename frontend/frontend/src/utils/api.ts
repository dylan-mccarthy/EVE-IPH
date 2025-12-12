const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5056';

export interface ApiError {
  code: string;
  message: string;
}

export interface Asset {
  itemId: number;
  locationId: number;
  locationFlag: string;
  locationType: string;
  typeId: number;
  quantity: number;
  isSingleton: boolean;
  isBlueprintCopy: boolean | null;
}

export interface IndustryJob {
  jobId: number;
  installerId: number;
  facilityId: number;
  locationId: number;
  activityId: number;
  blueprintId: number;
  blueprintTypeId: number;
  blueprintLocationId: number;
  outputLocationId: number;
  runs: number;
  cost: number;
  licensedRuns: number;
  probability: number;
  productTypeId: number | null;
  status: string;
  timeInSeconds: number;
  startDate: string;
  endDate: string;
  pauseDate: string | null;
  completedDate: string | null;
  completedCharacterId: number | null;
  successfulRuns: number | null;
}

export interface MarketOrder {
  orderId: number;
  typeId: number;
  locationId: number;
  regionId: number;
  volumeTotal: string;
  volumeRemain: string;
  minVolume: string;
  price: number;
  isBuyOrder: boolean;
  duration: string;
  issued: string;
  range: string;
}

export interface WalletTransaction {
  transactionId: number;
  date: string;
  typeId: number;
  locationId: number;
  quantity: number;
  unitPrice: number;
  clientId: number;
  isBuy: boolean;
  isPersonal: boolean;
  journalRefId: number;
}

export interface TypeInfo {
  typeId: number;
  typeName: string;
  groupName: string;
  categoryName: string;
}

export interface BlueprintSearchResponse {
  items: BlueprintSummary[];
  total: number;
  page: number;
  pageSize: number;
}

export interface BlueprintSummary {
  id: number;
  name: string;
  group: string;
  category: string;
}

export interface BlueprintDetails {
  blueprintId: number;
  blueprintName: string;
  itemGroup: string;
  itemCategory: string;
  activities: BlueprintActivity[];
}

export interface BlueprintActivity {
  activityId: number;
  activityName: string;
  productId: number;
  productName: string;
  productQuantity: number;
  materials: BlueprintMaterial[];
  products: BlueprintProduct[];
}

export interface BlueprintMaterial {
  materialId: number;
  materialName: string;
  materialGroup: string;
  materialCategory: string;
  quantity: number;
  volume: number;
  consume: boolean;
}

export interface BlueprintProduct {
  productId: number;
  productName: string;
  quantity: number;
  probability: number;
}

// Market Prices
export interface MarketPrice {
  typeId: number;
  regionId: number;
  buyPrice: number;
  sellPrice: number;
  volume: number;
  lastUpdated: string;
  expiresAt: string;
}

export interface MarketPriceResponse {
  prices: Record<number, MarketPrice>;
}

export interface RefreshPricesRequest {
  typeIds: number[];
  regionId: number;
}

export interface RefreshPricesResponse {
  updated: number;
  failed: number;
}

// Item Groups
export interface ItemGroup {
  groupId: number;
  groupName: string;
  categoryId: number;
  itemCount: number;
}

export interface ItemGroupsResponse {
  groups: ItemGroup[];
}

export interface ItemsByGroupResponse {
  totalItems: number;
  typeIds: number[];
}

// Manufacturing
export interface ManufacturingRequest {
  blueprintId: number;
  materialEfficiency?: number;
  timeEfficiency?: number;
  totalUnits?: number;
  runsPerBlueprint?: number;
  numberOfBlueprints?: number;
  productionLines?: number;
  facilityMaterialMultiplier?: number;
  facilityTimeMultiplier?: number;
  salesTaxRate?: number;
  brokerFeeRate?: number;
  jobInstallationCost?: number;
  materialMarketMode?: 'Buy' | 'BuyOrder';
  productMarketMode?: 'Buy' | 'SellOrder' | 'BuyOrder' | 'Sell';
  profitCostBasis?: 'Components' | 'Raw' | 'BuildBuy';
  sellExcessItems?: boolean;
  systemCostIndex?: number;
  facilityCostMultiplier?: number;
  facilityTaxRate?: number;
  sccSurchargeRate?: number;
  regionId?: number;
}

export interface ManufacturingLineItem {
  typeId: number;
  typeName: string;
  quantity: number;
  unitPrice: number;
  totalCost: number;
  missingPrice: boolean;
}

export interface ManufacturingResponse {
  blueprintId: number;
  blueprintName: string;
  regionId: number;
  materialEfficiency: number;
  timeEfficiency: number;
  totalUnits: number;
  componentMaterials: ManufacturingLineItem[];
  rawMaterials: ManufacturingLineItem[];
  componentTotalCost: number;
  rawTotalCost: number;
  buildBuyTotalCost: number | null;
  productValue: number;
  salesTax: number;
  brokerFee: number;
  jobInstallationCost: number;
  totalEiv: number;
  totalTimeSeconds: number;
  profitComponents: number;
  profitRaw: number;
  profitBuildBuy: number | null;
  profit: number;
  iphComponents: number;
  iphRaw: number;
  iphBuildBuy: number | null;
  iph: number;
  excessSellValueNet: number | null;
  warnings: string[];
}

async function handleResponse<T = unknown>(response: Response): Promise<T> {
  if (response.status === 401) {
    localStorage.removeItem('eveAuth');
    window.location.href = '/';
    throw new Error('Unauthorized');
  }

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({ code: 'unknown', message: response.statusText }));
    throw new Error(errorData.message || `Request failed: ${response.statusText}`);
  }

  return response.json();
}

export const api = {
  // Character endpoints
  characters: {
    getAll: () => fetch(`${API_BASE}/api/characters`).then(handleResponse),
    getDetails: (characterId: number) => 
      fetch(`${API_BASE}/api/characters/${characterId}`).then(handleResponse),
    getSkills: (characterId: number) =>
      fetch(`${API_BASE}/api/characters/${characterId}/skills`).then(handleResponse),
    sync: (characterId: number) =>
      fetch(`${API_BASE}/api/characters/${characterId}/sync`, { 
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
      }).then(handleResponse),
  },

  // Assets endpoints
  assets: {
    get: (characterId: number): Promise<Asset[]> =>
      fetch(`${API_BASE}/api/characters/${characterId}/assets`).then(handleResponse<Asset[]>),
  },

  // Industry endpoints
  industry: {
    getJobs: (characterId: number, includeCompleted: boolean = true): Promise<IndustryJob[]> =>
      fetch(`${API_BASE}/api/characters/${characterId}/industry/jobs?includeCompleted=${includeCompleted}`)
        .then(handleResponse<IndustryJob[]>),
  },

  // Manufacturing endpoints
  manufacturing: {
    calculate: (request: ManufacturingRequest): Promise<ManufacturingResponse> =>
      fetch(`${API_BASE}/api/manufacturing/calculate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      }).then(handleResponse<ManufacturingResponse>),
  },

  // Wallet endpoints
  wallet: {
    getBalance: (characterId: number) =>
      fetch(`${API_BASE}/api/characters/${characterId}/wallet`).then(handleResponse),
    getTransactions: (characterId: number): Promise<WalletTransaction[]> =>
      fetch(`${API_BASE}/api/characters/${characterId}/wallet/transactions`).then(handleResponse<WalletTransaction[]>),
    getJournal: (characterId: number) =>
      fetch(`${API_BASE}/api/characters/${characterId}/wallet/journal`).then(handleResponse),
  },

  // SDE (Static Data Export) endpoints
  sde: {
    getTypeInfo: (typeId: number): Promise<TypeInfo> =>
      fetch(`${API_BASE}/api/sde/types/${typeId}`).then(handleResponse<TypeInfo>),
    getTypeInfoBatch: (typeIds: number[]): Promise<Record<number, TypeInfo>> =>
      fetch(`${API_BASE}/api/sde/types/batch`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ typeIds }),
      }).then(handleResponse<Record<number, TypeInfo>>),
    getLocationName: (locationId: number): Promise<{ name: string }> =>
      fetch(`${API_BASE}/api/sde/locations/${locationId}`).then(handleResponse<{ name: string }>),
    getActivityName: (activityId: number): Promise<{ name: string }> =>
      fetch(`${API_BASE}/api/sde/activities/${activityId}`).then(handleResponse<{ name: string }>),
  },

  // Blueprint endpoints
  blueprints: {
    search: (params: {
      query?: string;
      group?: string;
      category?: string;
      page?: number;
      pageSize?: number;
    }): Promise<BlueprintSearchResponse> => {
      const searchParams = new URLSearchParams();
      if (params.query) searchParams.append('query', params.query);
      if (params.group) searchParams.append('group', params.group);
      if (params.category) searchParams.append('category', params.category);
      if (params.page) searchParams.append('page', params.page.toString());
      if (params.pageSize) searchParams.append('pageSize', params.pageSize.toString());
      
      return fetch(`${API_BASE}/api/blueprints/search?${searchParams}`)
        .then(handleResponse<BlueprintSearchResponse>);
    },
    getDetails: (blueprintId: number): Promise<BlueprintDetails> =>
      fetch(`${API_BASE}/api/blueprints/${blueprintId}`).then(handleResponse<BlueprintDetails>),
  },

  // Market endpoints (orders + cached prices + refresh + groups)
  market: {
    getOrders: (characterId: number): Promise<MarketOrder[]> =>
      fetch(`${API_BASE}/api/characters/${characterId}/orders`).then(handleResponse<MarketOrder[]>),

    getPrices: (typeIds: number[], regionId: number = 10000002): Promise<MarketPriceResponse> => {
      const searchParams = new URLSearchParams({
        typeIds: typeIds.join(','),
        regionId: regionId.toString(),
      });
      return fetch(`${API_BASE}/api/market/prices/cached?${searchParams}`)
        .then(handleResponse<MarketPriceResponse>);
    },
    getPrice: (typeId: number, regionId: number = 10000002): Promise<MarketPrice> => {
      const searchParams = new URLSearchParams({
        regionId: regionId.toString(),
      });
      return fetch(`${API_BASE}/api/market/prices/cached/${typeId}?${searchParams}`)
        .then(handleResponse<MarketPrice>);
    },
    refreshPrices: (typeIds: number[], regionId: number = 10000002): Promise<RefreshPricesResponse> =>
      fetch(`${API_BASE}/api/market/prices/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ typeIds, regionId }),
      }).then(handleResponse<RefreshPricesResponse>),
    
    getItemGroups: (): Promise<ItemGroupsResponse> =>
      fetch(`${API_BASE}/api/market/groups`).then(handleResponse<ItemGroupsResponse>),
    
    getItemsByGroups: (groupIds: number[], regionId: number = 10000002): Promise<ItemsByGroupResponse> =>
      fetch(`${API_BASE}/api/market/groups/items`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ groupIds, regionId }),
      }).then(handleResponse<ItemsByGroupResponse>),
  },
};

export default api;
