import { useState, useEffect, useRef } from 'react';
import api, { type BlueprintSummary, type BlueprintDetails, type MarketPrice } from '../utils/api';
import './Blueprints.css';

const Blueprints = () => {
  const [searchResults, setSearchResults] = useState<BlueprintSummary[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [searching, setSearching] = useState(false);
  const [showDropdown, setShowDropdown] = useState(false);
  const [selectedBlueprint, setSelectedBlueprint] = useState<BlueprintDetails | null>(null);
  const [loadingDetails, setLoadingDetails] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const searchRef = useRef<HTMLDivElement>(null);
  const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // Manufacturing settings state
  const [me, setMe] = useState(0);
  const [te, setTe] = useState(0);
  const [runsPerBp, setRunsPerBp] = useState(1);
  const [numBps, setNumBps] = useState(1);
  const [productionLines, setProductionLines] = useState(1);
  const [totalUnits, setTotalUnits] = useState(1);

  // Market prices state
  const [marketPrices, setMarketPrices] = useState<Record<number, MarketPrice>>({});
  const [loadingPrices, setLoadingPrices] = useState(false);
  const [priceError, setPriceError] = useState<string | null>(null);

  // Calculate adjusted material quantity based on ME
  const calculateMaterialQuantity = (baseQuantity: number, meLevel: number): number => {
    // ME reduces material requirements by 1% per level
    const reduction = 1 - (meLevel * 0.01);
    return Math.ceil(baseQuantity * reduction * totalUnits);
  };

  // Format ISK value
  const formatISK = (value: number): string => {
    if (value >= 1_000_000_000) {
      return `${(value / 1_000_000_000).toFixed(2)}B ISK`;
    } else if (value >= 1_000_000) {
      return `${(value / 1_000_000).toFixed(2)}M ISK`;
    } else if (value >= 1_000) {
      return `${(value / 1_000).toFixed(2)}K ISK`;
    }
    return `${value.toFixed(2)} ISK`;
  };

  // Calculate total component cost
  const calculateComponentCost = (): number => {
    if (!selectedBlueprint) return 0;
    
    const mfgActivity = selectedBlueprint.activities.find(a => a.activityName === 'Manufacturing');
    if (!mfgActivity) return 0;

    return mfgActivity.materials.reduce((total, mat) => {
      const adjustedQty = calculateMaterialQuantity(mat.quantity, me);
      const price = marketPrices[mat.materialId];
      const unitPrice = price?.sellPrice || 0;
      return total + (adjustedQty * unitPrice);
    }, 0);
  };

  // Calculate product market value
  const calculateProductValue = (): number => {
    if (!selectedBlueprint) return 0;
    
    const mfgActivity = selectedBlueprint.activities.find(a => a.activityName === 'Manufacturing');
    if (!mfgActivity) return 0;

    const productPrice = marketPrices[mfgActivity.productId];
    const unitPrice = productPrice?.sellPrice || 0;
    return unitPrice * mfgActivity.productQuantity * totalUnits;
  };

  // Fetch cached market prices when blueprint is selected (no ESI refresh)
  const fetchMarketPrices = async (blueprint: BlueprintDetails) => {
    const mfgActivity = blueprint.activities.find(a => a.activityName === 'Manufacturing');
    if (!mfgActivity) return;

    const typeIds = [
      mfgActivity.productId,
      ...mfgActivity.materials.map(m => m.materialId)
    ];

    try {
      setLoadingPrices(true);
      setPriceError(null);
      
      // Only get cached prices - never trigger ESI refresh
      const cachedResponse = await api.market.getPrices(typeIds);
      setMarketPrices(cachedResponse.prices);
      
      // Check if we have prices for all items
      const missingPrices = typeIds.filter(id => !cachedResponse.prices[id]);
      
      if (missingPrices.length > 0) {
        setPriceError(`Missing prices for ${missingPrices.length} item(s). Update prices from the Market Data page.`);
      }
    } catch (err) {
      console.error('Error fetching market prices:', err);
      setPriceError(err instanceof Error ? err.message : 'Failed to fetch market prices');
    } finally {
      setLoadingPrices(false);
    }
  };

  // Live search as user types
  useEffect(() => {
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }

    if (!searchTerm.trim()) {
      setSearchResults([]);
      setShowDropdown(false);
      return;
    }

    searchTimeoutRef.current = setTimeout(async () => {
      try {
        setSearching(true);
        setError(null);
        const response = await api.blueprints.search({
          query: searchTerm,
          page: 1,
          pageSize: 15,
        });
        setSearchResults(response.items);
        setShowDropdown(response.items.length > 0);
      } catch (err) {
        console.error('Error searching blueprints:', err);
        setError(err instanceof Error ? err.message : 'Failed to search blueprints');
      } finally {
        setSearching(false);
      }
    }, 300); // Debounce for 300ms

    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
  }, [searchTerm]);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (searchRef.current && !searchRef.current.contains(event.target as Node)) {
        setShowDropdown(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleSelectBlueprint = async (blueprint: BlueprintSummary) => {
    try {
      setLoadingDetails(true);
      setError(null);
      setShowDropdown(false);
      const details = await api.blueprints.getDetails(blueprint.id);
      setSelectedBlueprint(details);
      setSearchTerm(blueprint.name); // Update search term to selected blueprint name
      
      // Fetch market prices for the blueprint materials
      await fetchMarketPrices(details);
    } catch (err) {
      console.error('Error fetching blueprint details:', err);
      setError(err instanceof Error ? err.message : 'Failed to fetch blueprint details');
    } finally {
      setLoadingDetails(false);
    }
  };

  const handleNewSearch = () => {
    setSelectedBlueprint(null);
    setSearchTerm('');
    setSearchResults([]);
    setShowDropdown(false);
    // Reset manufacturing settings
    setMe(0);
    setTe(0);
    setRunsPerBp(1);
    setNumBps(1);
    setProductionLines(1);
    setTotalUnits(1);
    // Clear market prices
    setMarketPrices({});
    setPriceError(null);
  };

  return (
    <div className="blueprints-container">
      <div className="blueprints-header">
        <h1>Manufacturing Calculator</h1>
        {selectedBlueprint && (
          <button className="btn-new-search" onClick={handleNewSearch}>
            New Search
          </button>
        )}
      </div>

      {error && (
        <div className="error-message">
          <p>{error}</p>
        </div>
      )}

      {!selectedBlueprint ? (
        <>
          {/* Search Interface with Dropdown */}
          <div className="search-section" ref={searchRef}>
            <div className="search-wrapper">
              <input
                type="text"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Search for a blueprint..."
                className="search-input-large"
                autoFocus
              />
              {searching && <div className="search-loading">Searching...</div>}
              
              {/* Dropdown Results */}
              {showDropdown && searchResults.length > 0 && (
                <div className="search-dropdown">
                  {searchResults.map((bp) => (
                    <div
                      key={bp.id}
                      className="dropdown-item"
                      onClick={() => handleSelectBlueprint(bp)}
                    >
                      <img
                        src={`https://images.evetech.net/types/${bp.id}/bp?size=32`}
                        alt={bp.name}
                        className="dropdown-icon"
                      />
                      <div className="dropdown-info">
                        <div className="dropdown-name">{bp.name}</div>
                        <div className="dropdown-meta">{bp.group} • {bp.category}</div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </>
      ) : (
        <>
          {/* Blueprint Details */}
          {loadingDetails ? (
            <div className="loading">Loading blueprint details...</div>
          ) : selectedBlueprint ? (
            <div className="blueprint-details">
              {/* Blueprint Header */}
              <div className="details-header">
                <img
                  src={`https://images.evetech.net/types/${selectedBlueprint.blueprintId}/bp?size=128`}
                  alt={selectedBlueprint.blueprintName}
                  className="details-icon"
                />
                <div className="details-title">
                  <h2>{selectedBlueprint.blueprintName}</h2>
                  <p className="details-meta">
                    {selectedBlueprint.itemGroup} • {selectedBlueprint.itemCategory}
                  </p>
                </div>
              </div>

              {/* Manufacturing Controls */}
              <div className="manufacturing-controls">
                <div className="control-group">
                  <label>ME (Material Efficiency)</label>
                  <input 
                    type="number" 
                    value={me} 
                    onChange={(e) => setMe(Math.max(0, Math.min(10, Number(e.target.value))))}
                    min="0" 
                    max="10" 
                  />
                </div>
                <div className="control-group">
                  <label>TE (Time Efficiency)</label>
                  <input 
                    type="number" 
                    value={te}
                    onChange={(e) => setTe(Math.max(0, Math.min(20, Number(e.target.value))))}
                    min="0" 
                    max="20" 
                  />
                </div>
                <div className="control-group">
                  <label>Runs per BP</label>
                  <input 
                    type="number" 
                    value={runsPerBp}
                    onChange={(e) => setRunsPerBp(Math.max(1, Number(e.target.value)))}
                    min="1" 
                  />
                </div>
                <div className="control-group">
                  <label>Number of BPs</label>
                  <input 
                    type="number" 
                    value={numBps}
                    onChange={(e) => setNumBps(Math.max(1, Number(e.target.value)))}
                    min="1" 
                  />
                </div>
                <div className="control-group">
                  <label>Production Lines</label>
                  <input 
                    type="number" 
                    value={productionLines}
                    onChange={(e) => setProductionLines(Math.max(1, Number(e.target.value)))}
                    min="1" 
                  />
                </div>
                <div className="control-group">
                  <label>Total Units</label>
                  <input 
                    type="number" 
                    value={totalUnits}
                    onChange={(e) => setTotalUnits(Math.max(1, Number(e.target.value)))}
                    min="1" 
                    className="highlight-input" 
                  />
                </div>
              </div>

              {/* Cost Summary */}
              <div className="cost-summary">
                <div className="summary-card">
                  <h4>Component Material Cost</h4>
                  <div className="cost-value">
                    {loadingPrices ? 'Loading...' : formatISK(calculateComponentCost())}
                  </div>
                  <div className="cost-label">
                    {loadingPrices ? 'Fetching prices...' : 'Jita sell prices'}
                  </div>
                </div>
                <div className="summary-card">
                  <h4>Raw Material Cost</h4>
                  <div className="cost-value">
                    {loadingPrices ? 'Loading...' : formatISK(calculateComponentCost())}
                  </div>
                  <div className="cost-label">
                    {loadingPrices ? 'Fetching prices...' : 'Same as components'}
                  </div>
                </div>
                <div className="summary-card">
                  <h4>Market Price</h4>
                  <div className="cost-value">
                    {loadingPrices ? 'Loading...' : formatISK(calculateProductValue())}
                  </div>
                  <div className="cost-label">Product sell price</div>
                </div>
                <div className="summary-card profit">
                  <h4>Profit</h4>
                  <div className="cost-value">
                    {loadingPrices ? 'Loading...' : formatISK(calculateProductValue() - calculateComponentCost())}
                  </div>
                  <div className="cost-label">Market - Cost</div>
                </div>
              </div>

              {/* Price Information */}
              {priceError && (
                <div className="price-error">
                  <p>⚠️ {priceError}</p>
                  <p style={{ fontSize: '0.85rem', marginTop: '8px' }}>
                    Go to <a href="/market-data" style={{ color: '#667eea', textDecoration: 'underline' }}>Market Data</a> to sync prices from Jita.
                  </p>
                </div>
              )}
              {Object.keys(marketPrices).length > 0 && marketPrices[Object.keys(marketPrices)[0] as any] && (
                <div className="price-info">
                  <span className="price-timestamp">
                    📊 Using cached prices from {new Date(marketPrices[Object.keys(marketPrices)[0] as any].lastUpdated).toLocaleString()}
                  </span>
                  <span className="price-note">
                    (Update prices from the <a href="/market-data" style={{ color: '#667eea' }}>Market Data</a> page)
                  </span>
                </div>
              )}

              {/* Materials Lists */}
              <div className="materials-container">
                {selectedBlueprint.activities
                  .filter(activity => activity.activityName === 'Manufacturing')
                  .map((activity) => (
                    <div key={activity.activityName} className="materials-wrapper">
                      {/* Component Materials */}
                      <div className="materials-column">
                        <h3>Component Materials</h3>
                        <table className="materials-table compact">
                          <thead>
                            <tr>
                              <th>Material</th>
                              <th>Qty</th>
                              <th>Cost/Item</th>
                              <th>Total Cost</th>
                            </tr>
                          </thead>
                          <tbody>
                            {activity.materials.map((mat) => {
                              const adjustedQty = calculateMaterialQuantity(mat.quantity, me);
                              const price = marketPrices[mat.materialId];
                              const unitPrice = price?.sellPrice || 0;
                              const totalCost = adjustedQty * unitPrice;
                              return (
                                <tr key={mat.materialId}>
                                  <td className="material-name">
                                    <img
                                      src={`https://images.evetech.net/types/${mat.materialId}/icon?size=32`}
                                      alt={mat.materialName}
                                      className="material-icon"
                                    />
                                    <span>{mat.materialName}</span>
                                  </td>
                                  <td className="quantity">{adjustedQty.toLocaleString()}</td>
                                  <td className="price">
                                    {loadingPrices ? '...' : unitPrice > 0 ? unitPrice.toFixed(2) : '-'}
                                  </td>
                                  <td className="total-cost">
                                    {loadingPrices ? '...' : totalCost > 0 ? formatISK(totalCost) : '-'}
                                  </td>
                                </tr>
                              );
                            })}
                          </tbody>
                        </table>
                      </div>

                      {/* Raw Materials */}
                      <div className="materials-column">
                        <h3>Raw Materials</h3>
                        <table className="materials-table compact">
                          <thead>
                            <tr>
                              <th>Material</th>
                              <th>Qty</th>
                              <th>Cost/Item</th>
                              <th>Total Cost</th>
                            </tr>
                          </thead>
                          <tbody>
                            {activity.materials.map((mat) => {
                              const adjustedQty = calculateMaterialQuantity(mat.quantity, me);
                              const price = marketPrices[mat.materialId];
                              const unitPrice = price?.sellPrice || 0;
                              const totalCost = adjustedQty * unitPrice;
                              return (
                                <tr key={`raw-${mat.materialId}`}>
                                  <td className="material-name">
                                    <img
                                      src={`https://images.evetech.net/types/${mat.materialId}/icon?size=32`}
                                      alt={mat.materialName}
                                      className="material-icon"
                                    />
                                    <span>{mat.materialName}</span>
                                  </td>
                                  <td className="quantity">{adjustedQty.toLocaleString()}</td>
                                  <td className="price">
                                    {loadingPrices ? '...' : unitPrice > 0 ? unitPrice.toFixed(2) : '-'}
                                  </td>
                                  <td className="total-cost">
                                    {loadingPrices ? '...' : totalCost > 0 ? formatISK(totalCost) : '-'}
                                  </td>
                                </tr>
                              );
                            })}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  ))}
              </div>

              {/* Additional Activities (Invention, etc.) */}
              {selectedBlueprint.activities.filter(a => a.activityName !== 'Manufacturing').length > 0 && (
                <div className="other-activities">
                  <h3>Other Activities</h3>
                  {selectedBlueprint.activities
                    .filter(activity => activity.activityName !== 'Manufacturing')
                    .map((activity) => (
                      <details key={activity.activityName} className="activity-details">
                        <summary>{activity.activityName}</summary>
                        <div className="activity-content">
                          {activity.materials.length > 0 && (
                            <div>
                              <h4>Materials</h4>
                              <ul className="simple-list">
                                {activity.materials.map((mat) => (
                                  <li key={mat.materialId}>
                                    {mat.materialName}: {mat.quantity.toLocaleString()}
                                  </li>
                                ))}
                              </ul>
                            </div>
                          )}
                          {activity.products.length > 0 && (
                            <div>
                              <h4>Products</h4>
                              <ul className="simple-list">
                                {activity.products.map((prod) => (
                                  <li key={prod.productId}>
                                    {prod.productName}: {prod.quantity} ({(prod.probability * 100).toFixed(1)}%)
                                  </li>
                                ))}
                              </ul>
                            </div>
                          )}
                        </div>
                      </details>
                    ))}
                </div>
              )}
            </div>
          ) : null}
        </>
      )}
    </div>
  );
};

export default Blueprints;
