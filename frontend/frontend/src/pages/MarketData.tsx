import React, { useState, useEffect } from 'react';
import api from '../utils/api';
import './MarketData.css';

interface ItemGroup {
  groupId: number;
  groupName: string;
  categoryId: number;
  itemCount: number;
}

interface PriceUpdateResult {
  typeId: number;
  typeName: string;
  success: boolean;
  buyPrice?: number;
  sellPrice?: number;
  error?: string;
}

const MarketData: React.FC = () => {
  const [itemGroups, setItemGroups] = useState<ItemGroup[]>([]);
  const [selectedGroups, setSelectedGroups] = useState<Set<number>>(new Set());
  const [searchFilter, setSearchFilter] = useState<string>('');
  const [loading, setLoading] = useState(false);
  const [loadingGroups, setLoadingGroups] = useState(true);
  const [results, setResults] = useState<PriceUpdateResult[]>([]);
  const [summary, setSummary] = useState<{ updated: number; failed: number } | null>(null);

  const regionId = 10000002; // The Forge (Jita)
  const regionName = 'The Forge';

  // Load item groups on mount
  useEffect(() => {
    const loadGroups = async () => {
      try {
        const response = await api.market.getItemGroups();
        setItemGroups(response.groups);
      } catch (error) {
        console.error('Error loading item groups:', error);
        alert('Failed to load item groups');
      } finally {
        setLoadingGroups(false);
      }
    };

    loadGroups();
  }, []);

  const filteredGroups = itemGroups.filter(group =>
    group.groupName.toLowerCase().includes(searchFilter.toLowerCase())
  );

  const handleGroupToggle = (groupId: number) => {
    const newSelected = new Set(selectedGroups);
    if (newSelected.has(groupId)) {
      newSelected.delete(groupId);
    } else {
      newSelected.add(groupId);
    }
    setSelectedGroups(newSelected);
  };

  const handleSelectAll = () => {
    setSelectedGroups(new Set(filteredGroups.map(g => g.groupId)));
  };

  const handleDeselectAll = () => {
    setSelectedGroups(new Set());
  };

  const handleSelectCategory = (category: string) => {
    // Common material categories for quick selection
    const categoryMap: Record<string, string[]> = {
      'minerals': ['Veldspar', 'Scordite', 'Pyroxeres', 'Plagioclase', 'Omber', 'Kernite', 'Jaspet', 'Hemorphite', 'Hedbergite', 'Gneiss', 'Dark Ochre', 'Spodumain', 'Crokite', 'Bistot', 'Arkonor', 'Mercoxit'],
      'ice': ['Ice Products'],
      'moon': ['Moon Materials', 'Intermediate Materials', 'Composite'],
      'salvage': ['Salvage', 'Ancient Salvage'],
      'planetary': ['Planet', 'Commodities'],
    };

    const patterns = categoryMap[category] || [];
    const matching = itemGroups.filter(g =>
      patterns.some(pattern => g.groupName.includes(pattern))
    );
    setSelectedGroups(new Set(matching.map(g => g.groupId)));
  };

  const handleFetchPrices = async () => {
    if (selectedGroups.size === 0) {
      alert('Please select at least one item group');
      return;
    }

    setLoading(true);
    setResults([]);
    setSummary(null);

    try {
      // Get type IDs for selected groups
      const itemsResponse = await api.market.getItemsByGroups(Array.from(selectedGroups), regionId);
      const typeIds = itemsResponse.typeIds;

      if (typeIds.length === 0) {
        alert('No items found in selected groups');
        setLoading(false);
        return;
      }

      // Refresh prices from ESI
      const refreshResponse = await api.market.refreshPrices(typeIds, regionId);
      setSummary(refreshResponse);

      // Fetch the cached prices
      const pricesResponse = await api.market.getPrices(typeIds, regionId);
      
      // Get type names for display
      const typeInfoBatch = await api.sde.getTypeInfoBatch(typeIds);

      // Build results array
      const resultsArray: PriceUpdateResult[] = typeIds.map(typeId => {
        const price = pricesResponse.prices[typeId];
        const typeInfo = typeInfoBatch[typeId];
        
        if (price) {
          return {
            typeId,
            typeName: typeInfo?.typeName || `Type ${typeId}`,
            success: true,
            buyPrice: price.buyPrice,
            sellPrice: price.sellPrice,
          };
        } else {
          return {
            typeId,
            typeName: typeInfo?.typeName || `Type ${typeId}`,
            success: false,
            error: 'No market data available',
          };
        }
      });

      setResults(resultsArray);
    } catch (error) {
      console.error('Error fetching market prices:', error);
      alert(`Error: ${error instanceof Error ? error.message : 'Failed to fetch prices'}`);
    } finally {
      setLoading(false);
    }
  };

  const formatISK = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(amount);
  };

  const totalItemsSelected = itemGroups
    .filter(g => selectedGroups.has(g.groupId))
    .reduce((sum, g) => sum + g.itemCount, 0);

  return (
    <div className="market-data-page">
      <div className="page-header">
        <h1>Market Data Management</h1>
        <p className="region-info">Region: {regionName} (ID: {regionId})</p>
      </div>

      <div className="main-content">
        <div className="groups-section">
          <div className="groups-header">
            <h2>Item Groups</h2>
            <div className="search-box">
              <input
                type="text"
                placeholder="Search groups..."
                value={searchFilter}
                onChange={(e) => setSearchFilter(e.target.value)}
                disabled={loading}
              />
            </div>
          </div>

          <div className="selection-controls">
            <button onClick={handleSelectAll} disabled={loading} className="btn-small">
              Select All
            </button>
            <button onClick={handleDeselectAll} disabled={loading} className="btn-small">
              Deselect All
            </button>
            <div className="quick-select">
              <button onClick={() => handleSelectCategory('minerals')} disabled={loading} className="btn-small">
                Minerals
              </button>
              <button onClick={() => handleSelectCategory('ice')} disabled={loading} className="btn-small">
                Ice
              </button>
              <button onClick={() => handleSelectCategory('moon')} disabled={loading} className="btn-small">
                Moon Materials
              </button>
              <button onClick={() => handleSelectCategory('salvage')} disabled={loading} className="btn-small">
                Salvage
              </button>
            </div>
          </div>

          {loadingGroups ? (
            <div className="loading-message">Loading item groups...</div>
          ) : (
            <div className="groups-list">
              {filteredGroups.map((group) => (
                <label key={group.groupId} className="group-checkbox">
                  <input
                    type="checkbox"
                    checked={selectedGroups.has(group.groupId)}
                    onChange={() => handleGroupToggle(group.groupId)}
                    disabled={loading}
                  />
                  <span className="group-name">{group.groupName}</span>
                  <span className="group-count">({group.itemCount})</span>
                </label>
              ))}
            </div>
          )}

          <div className="groups-footer">
            <div className="selection-summary">
              <strong>{selectedGroups.size}</strong> groups selected
              ({totalItemsSelected} items)
            </div>
            <button
              onClick={handleFetchPrices}
              disabled={loading || selectedGroups.size === 0}
              className="btn-primary btn-fetch"
            >
              {loading ? 'Fetching Prices...' : 'Update Market Prices'}
            </button>
          </div>
        </div>

        <div className="results-section">
          {summary && (
            <div className="summary-card">
              <h3>Update Summary</h3>
              <div className="summary-stats">
                <div className="stat success">
                  <span className="label">Updated:</span>
                  <span className="value">{summary.updated}</span>
                </div>
                <div className="stat failed">
                  <span className="label">Failed:</span>
                  <span className="value">{summary.failed}</span>
                </div>
              </div>
            </div>
          )}

          {results.length > 0 && (
            <div className="prices-card">
              <h3>Price Data ({results.length} items)</h3>
              <div className="table-container">
                <table className="price-table">
                  <thead>
                    <tr>
                      <th>Type ID</th>
                      <th>Item Name</th>
                      <th>Buy Price</th>
                      <th>Sell Price</th>
                      <th>Spread</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {results.map((result) => (
                      <tr key={result.typeId} className={result.success ? 'success' : 'error'}>
                        <td>{result.typeId}</td>
                        <td>{result.typeName}</td>
                        <td className="price">
                          {result.buyPrice !== undefined
                            ? `${formatISK(result.buyPrice)} ISK`
                            : '-'}
                        </td>
                        <td className="price">
                          {result.sellPrice !== undefined
                            ? `${formatISK(result.sellPrice)} ISK`
                            : '-'}
                        </td>
                        <td className="price">
                          {result.buyPrice !== undefined && result.sellPrice !== undefined
                            ? `${formatISK(result.sellPrice - result.buyPrice)} ISK`
                            : '-'}
                        </td>
                        <td>
                          {result.success ? (
                            <span className="status-success">✓ Updated</span>
                          ) : (
                            <span className="status-error">✗ {result.error}</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {!summary && results.length === 0 && (
            <div className="placeholder-message">
              <p>Select item groups and click "Update Market Prices" to fetch data from ESI</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default MarketData;
