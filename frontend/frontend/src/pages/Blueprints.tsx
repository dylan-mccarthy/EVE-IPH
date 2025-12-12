import { useState, useEffect, useRef } from 'react';
import api, { type BlueprintSummary, type BlueprintDetails, type ManufacturingResponse } from '../utils/api';
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
  const searchTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Manufacturing settings state
  const [me, setMe] = useState(0);
  const [te, setTe] = useState(0);
  const [runsPerBp, setRunsPerBp] = useState(1);
  const [numBps, setNumBps] = useState(1);
  const [productionLines, setProductionLines] = useState(1);
  const [totalUnits, setTotalUnits] = useState(1);

  // Optional parity inputs (default values preserve existing behavior)
  const [facilityMaterialMultiplier, setFacilityMaterialMultiplier] = useState(1);
  const [facilityTimeMultiplier, setFacilityTimeMultiplier] = useState(1);
  const [salesTaxRate, setSalesTaxRate] = useState(0);
  const [brokerFeeRate, setBrokerFeeRate] = useState(0);
  const [jobInstallationCost, setJobInstallationCost] = useState(0);
  const [materialMarketMode, setMaterialMarketMode] = useState<'Buy' | 'BuyOrder'>('Buy');
  const [productMarketMode, setProductMarketMode] = useState<'Buy' | 'SellOrder' | 'BuyOrder' | 'Sell'>('SellOrder');
  const [profitCostBasis, setProfitCostBasis] = useState<'Components' | 'Raw' | 'BuildBuy'>('Components');
  const [sellExcessItems, setSellExcessItems] = useState(false);
  const [systemCostIndex, setSystemCostIndex] = useState(0);
  const [facilityCostMultiplier, setFacilityCostMultiplier] = useState(1);
  const [facilityTaxRate, setFacilityTaxRate] = useState(0);
  const [sccSurchargeRate, setSccSurchargeRate] = useState(0);

  // Server-side manufacturing calculation state
  const [manufacturing, setManufacturing] = useState<ManufacturingResponse | null>(null);
  const [loadingCalc, setLoadingCalc] = useState(false);
  const [calcError, setCalcError] = useState<string | null>(null);

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

  const formatDuration = (seconds: number): string => {
    if (!Number.isFinite(seconds) || seconds <= 0) return '0s';

    const totalSeconds = Math.floor(seconds);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const secs = totalSeconds % 60;

    if (hours > 0) return `${hours}h ${minutes}m ${secs}s`;
    if (minutes > 0) return `${minutes}m ${secs}s`;
    return `${secs}s`;
  };

  const calculateManufacturing = async (blueprintId: number) => {
    try {
      setLoadingCalc(true);
      setCalcError(null);

      const result = await api.manufacturing.calculate({
        blueprintId,
        materialEfficiency: me,
        timeEfficiency: te,
        totalUnits,
        runsPerBlueprint: runsPerBp,
        numberOfBlueprints: numBps,
        productionLines,
        facilityMaterialMultiplier,
        facilityTimeMultiplier,
        salesTaxRate,
        brokerFeeRate,
        jobInstallationCost,
        materialMarketMode,
        productMarketMode,
        profitCostBasis,
        sellExcessItems,
        systemCostIndex,
        facilityCostMultiplier,
        facilityTaxRate,
        sccSurchargeRate,
        regionId: 10000002,
      });

      setManufacturing(result);
    } catch (err) {
      console.error('Error calculating manufacturing:', err);
      setCalcError(err instanceof Error ? err.message : 'Failed to calculate manufacturing');
      setManufacturing(null);
    } finally {
      setLoadingCalc(false);
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
    setFacilityMaterialMultiplier(1);
    setFacilityTimeMultiplier(1);
    setSalesTaxRate(0);
    setBrokerFeeRate(0);
    setJobInstallationCost(0);
    setSystemCostIndex(0);
    setFacilityCostMultiplier(1);
    setFacilityTaxRate(0);
    setSccSurchargeRate(0);
    // Clear server-side calculation
    setManufacturing(null);
    setCalcError(null);
  };

  // Recalculate on input changes (debounced)
  useEffect(() => {
    if (!selectedBlueprint) return;

    const timeout = setTimeout(() => {
      calculateManufacturing(selectedBlueprint.blueprintId);
    }, 250);

    return () => clearTimeout(timeout);
  }, [
    selectedBlueprint,
    me,
    te,
    totalUnits,
    runsPerBp,
    numBps,
    productionLines,
    facilityMaterialMultiplier,
    facilityTimeMultiplier,
    salesTaxRate,
    brokerFeeRate,
    jobInstallationCost,
    systemCostIndex,
    facilityCostMultiplier,
    facilityTaxRate,
    sccSurchargeRate,
  ]);

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

                <div className="control-group">
                  <label>Facility Material Mult</label>
                  <input
                    type="number"
                    value={facilityMaterialMultiplier}
                    onChange={(e) => setFacilityMaterialMultiplier(Math.max(0, Number(e.target.value)))}
                    min="0"
                    step="0.01"
                  />
                </div>
                <div className="control-group">
                  <label>Facility Time Mult</label>
                  <input
                    type="number"
                    value={facilityTimeMultiplier}
                    onChange={(e) => setFacilityTimeMultiplier(Math.max(0, Number(e.target.value)))}
                    min="0"
                    step="0.01"
                  />
                </div>

                <div className="control-group">
                  <label>Sales Tax Rate (0-1)</label>
                  <input
                    type="number"
                    value={salesTaxRate}
                    onChange={(e) => setSalesTaxRate(Math.max(0, Math.min(1, Number(e.target.value))))}
                    min="0"
                    max="1"
                    step="0.001"
                  />
                </div>
                <div className="control-group">
                  <label>Broker Fee Rate (0-1)</label>
                  <input
                    type="number"
                    value={brokerFeeRate}
                    onChange={(e) => setBrokerFeeRate(Math.max(0, Math.min(1, Number(e.target.value))))}
                    min="0"
                    max="1"
                    step="0.001"
                  />
                </div>

                <div className="control-group">
                  <label>Material Market Mode</label>
                  <select
                    value={materialMarketMode}
                    onChange={(e) => setMaterialMarketMode(e.target.value as typeof materialMarketMode)}
                  >
                    <option value="Buy">Buy (min sell; no fees)</option>
                    <option value="BuyOrder">Buy Order (max buy; broker)</option>
                  </select>
                </div>

                <div className="control-group">
                  <label>Product Market Mode</label>
                  <select
                    value={productMarketMode}
                    onChange={(e) => setProductMarketMode(e.target.value as typeof productMarketMode)}
                  >
                    <option value="SellOrder">Sell Order (min sell; tax + broker)</option>
                    <option value="Sell">Sell (max buy; tax only)</option>
                    <option value="Buy">Buy (min sell; no fees)</option>
                    <option value="BuyOrder">Buy Order (max buy; broker only)</option>
                  </select>
                </div>

                <div className="control-group">
                  <label>Profit Cost Basis</label>
                  <select
                    value={profitCostBasis}
                    onChange={(e) => setProfitCostBasis(e.target.value as typeof profitCostBasis)}
                  >
                    <option value="Components">Components</option>
                    <option value="Raw">Raw</option>
                    <option value="BuildBuy">Build/Buy (auto)</option>
                  </select>
                </div>

                <div className="control-group">
                  <label>
                    <input
                      type="checkbox"
                      checked={sellExcessItems}
                      onChange={(e) => setSellExcessItems(e.target.checked)}
                      style={{ marginRight: '8px' }}
                    />
                    Sell Excess Items (net)
                  </label>
                </div>
                <div className="control-group">
                  <label>Job Installation Cost</label>
                  <input
                    type="number"
                    value={jobInstallationCost}
                    onChange={(e) => setJobInstallationCost(Math.max(0, Number(e.target.value)))}
                    min="0"
                    step="1"
                  />
                </div>

                <div className="control-group">
                  <label>System Cost Index (0-1)</label>
                  <input
                    type="number"
                    value={systemCostIndex}
                    onChange={(e) => setSystemCostIndex(Math.max(0, Math.min(1, Number(e.target.value))))}
                    min="0"
                    max="1"
                    step="0.0001"
                  />
                </div>
                <div className="control-group">
                  <label>Facility Cost Mult</label>
                  <input
                    type="number"
                    value={facilityCostMultiplier}
                    onChange={(e) => setFacilityCostMultiplier(Math.max(0, Number(e.target.value)))}
                    min="0"
                    step="0.01"
                  />
                </div>
                <div className="control-group">
                  <label>Facility Tax Rate (0-1)</label>
                  <input
                    type="number"
                    value={facilityTaxRate}
                    onChange={(e) => setFacilityTaxRate(Math.max(0, Math.min(1, Number(e.target.value))))}
                    min="0"
                    max="1"
                    step="0.001"
                  />
                </div>
                <div className="control-group">
                  <label>SCC Surcharge (0-1)</label>
                  <input
                    type="number"
                    value={sccSurchargeRate}
                    onChange={(e) => setSccSurchargeRate(Math.max(0, Math.min(1, Number(e.target.value))))}
                    min="0"
                    max="1"
                    step="0.001"
                  />
                </div>
              </div>

              {/* Cost Summary */}
              <div className="cost-summary">
                <div className="summary-card">
                  <h4>Component Material Cost</h4>
                  <div className="cost-value">
                    {loadingCalc ? 'Loading...' : formatISK(manufacturing?.componentTotalCost ?? 0)}
                  </div>
                  <div className="cost-label">
                    {loadingCalc ? 'Calculating...' : 'Cached sell prices'}
                  </div>
                </div>
                <div className="summary-card">
                  <h4>Raw Material Cost</h4>
                  <div className="cost-value">
                    {loadingCalc ? 'Loading...' : formatISK(manufacturing?.rawTotalCost ?? 0)}
                  </div>
                  <div className="cost-label">
                    {loadingCalc ? 'Calculating...' : 'Recursive breakdown'}
                  </div>
                </div>
                <div className="summary-card">
                  <h4>Market Price</h4>
                  <div className="cost-value">
                    {loadingCalc ? 'Loading...' : formatISK(manufacturing?.productValue ?? 0)}
                  </div>
                  <div className="cost-label">Product sell price</div>
                </div>
                <div className="summary-card profit">
                  <h4>Profit</h4>
                  <div className="cost-value">
                    {loadingCalc ? 'Loading...' : formatISK(manufacturing?.profit ?? 0)}
                  </div>
                  <div className="cost-label">Market - Cost - Fees</div>
                </div>

                <div className="summary-card">
                  <h4>Profit (Components)</h4>
                  <div className="cost-value">
                    {loadingCalc ? 'Loading...' : formatISK(manufacturing?.profitComponents ?? 0)}
                  </div>
                  <div className="cost-label">Market - Component Cost - Fees</div>
                </div>

                <div className="summary-card">
                  <h4>Profit (Raw)</h4>
                  <div className="cost-value">
                    {loadingCalc ? 'Loading...' : formatISK(manufacturing?.profitRaw ?? 0)}
                  </div>
                  <div className="cost-label">Market - Raw Cost - Fees</div>
                </div>

                <div className="summary-card">
                  <h4>IPH</h4>
                  <div className="cost-value">
                    {loadingCalc ? 'Loading...' : formatISK(manufacturing?.iph ?? 0)}
                  </div>
                  <div className="cost-label">ISK per hour</div>
                </div>

                <div className="summary-card">
                  <h4>IPH (Components)</h4>
                  <div className="cost-value">
                    {loadingCalc ? 'Loading...' : formatISK(manufacturing?.iphComponents ?? 0)}
                  </div>
                  <div className="cost-label">Profit components / time</div>
                </div>

                <div className="summary-card">
                  <h4>IPH (Raw)</h4>
                  <div className="cost-value">
                    {loadingCalc ? 'Loading...' : formatISK(manufacturing?.iphRaw ?? 0)}
                  </div>
                  <div className="cost-label">Profit raw / time</div>
                </div>

                <div className="summary-card">
                  <h4>Build Time</h4>
                  <div className="cost-value">
                    {loadingCalc ? 'Loading...' : formatDuration(manufacturing?.totalTimeSeconds ?? 0)}
                  </div>
                  <div className="cost-label">Adjusted for lines</div>
                </div>
              </div>

              {manufacturing && !loadingCalc ? (
                <div className="cost-summary">
                  <div className="summary-card">
                    <h4>Sales Tax</h4>
                    <div className="cost-value">{formatISK(manufacturing.salesTax ?? 0)}</div>
                    <div className="cost-label">Applied to product value</div>
                  </div>
                  <div className="summary-card">
                    <h4>Broker Fee</h4>
                    <div className="cost-value">{formatISK(manufacturing.brokerFee ?? 0)}</div>
                    <div className="cost-label">Applied to product value</div>
                  </div>
                  <div className="summary-card">
                    <h4>Job Cost</h4>
                    <div className="cost-value">{formatISK(manufacturing.jobInstallationCost ?? 0)}</div>
                    <div className="cost-label">Installation / index model</div>
                  </div>
                  <div className="summary-card">
                    <h4>EIV (Adj)</h4>
                    <div className="cost-value">{formatISK(manufacturing.totalEiv ?? 0)}</div>
                    <div className="cost-label">Adjusted-price based</div>
                  </div>

                  {manufacturing.buildBuyTotalCost !== null ? (
                    <div className="summary-card">
                      <h4>Build/Buy Cost</h4>
                      <div className="cost-value">{formatISK(manufacturing.buildBuyTotalCost ?? 0)}</div>
                      <div className="cost-label">Auto build vs buy</div>
                    </div>
                  ) : null}

                  {manufacturing.excessSellValueNet !== null ? (
                    <div className="summary-card">
                      <h4>Excess Sellback</h4>
                      <div className="cost-value">{formatISK(manufacturing.excessSellValueNet ?? 0)}</div>
                      <div className="cost-label">After tax + broker</div>
                    </div>
                  ) : null}
                </div>
              ) : null}

              {/* Calculation / Price Information */}
              {calcError && (
                <div className="price-error">
                  <p>⚠️ {calcError}</p>
                </div>
              )}
              {manufacturing?.warnings?.length ? (
                <div className="price-error">
                  <p>⚠️ {manufacturing.warnings[0]}</p>
                  <p style={{ fontSize: '0.85rem', marginTop: '8px' }}>
                    Go to <a href="/market-data" style={{ color: '#667eea', textDecoration: 'underline' }}>Market Data</a> to sync prices from Jita.
                  </p>
                </div>
              ) : null}

              {/* Materials Lists */}
              <div className="materials-container">
                {manufacturing ? (
                  <div className="materials-wrapper">
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
                          {manufacturing.componentMaterials.map((mat) => (
                            <tr key={mat.typeId}>
                              <td className="material-name">
                                <img
                                  src={`https://images.evetech.net/types/${mat.typeId}/icon?size=32`}
                                  alt={mat.typeName}
                                  className="material-icon"
                                />
                                <span>{mat.typeName}</span>
                              </td>
                              <td className="quantity">{mat.quantity.toLocaleString()}</td>
                              <td className="price">
                                {loadingCalc ? '...' : mat.unitPrice > 0 ? mat.unitPrice.toFixed(2) : '-'}
                              </td>
                              <td className="total-cost">
                                {loadingCalc ? '...' : mat.totalCost > 0 ? formatISK(mat.totalCost) : '-'}
                              </td>
                            </tr>
                          ))}
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
                          {manufacturing.rawMaterials.map((mat) => (
                            <tr key={`raw-${mat.typeId}`}>
                              <td className="material-name">
                                <img
                                  src={`https://images.evetech.net/types/${mat.typeId}/icon?size=32`}
                                  alt={mat.typeName}
                                  className="material-icon"
                                />
                                <span>{mat.typeName}</span>
                              </td>
                              <td className="quantity">{mat.quantity.toLocaleString()}</td>
                              <td className="price">
                                {loadingCalc ? '...' : mat.unitPrice > 0 ? mat.unitPrice.toFixed(2) : '-'}
                              </td>
                              <td className="total-cost">
                                {loadingCalc ? '...' : mat.totalCost > 0 ? formatISK(mat.totalCost) : '-'}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                ) : (
                  <div className="loading">Calculating manufacturing...</div>
                )}
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
