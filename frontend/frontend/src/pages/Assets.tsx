import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api, { type Asset, type TypeInfo } from '../utils/api';
import './Assets.css';

interface GroupedAssets {
  [locationId: number]: Asset[];
}

const Assets = () => {
  const [assets, setAssets] = useState<Asset[]>([]);
  const [typeInfoCache, setTypeInfoCache] = useState<Record<number, TypeInfo>>({});
  const [locationNames, setLocationNames] = useState<Record<number, string>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedLocation, setSelectedLocation] = useState<number | null>(null);
  const navigate = useNavigate();

  // Get character ID from localStorage
  const getCharacterId = (): number | null => {
    const authStr = localStorage.getItem('eveAuth');
    if (!authStr) return null;
    try {
      const auth = JSON.parse(authStr);
      return auth.characterId;
    } catch {
      return null;
    }
  };

  useEffect(() => {
    const characterId = getCharacterId();
    if (!characterId) {
      navigate('/characters');
      return;
    }

    const fetchAssets = async () => {
      try {
        setLoading(true);
        const data = await api.assets.get(characterId);
        setAssets(data);

        // Fetch type info for all unique type IDs
        const uniqueTypeIds = [...new Set(data.map(a => a.typeId))];
        if (uniqueTypeIds.length > 0) {
          const typeInfos = await api.sde.getTypeInfoBatch(uniqueTypeIds);
          setTypeInfoCache(typeInfos);
        }

        // Fetch location names for all unique location IDs
        const uniqueLocationIds = [...new Set(data.map(a => a.locationId))];
        const locationNamePromises = uniqueLocationIds.map(async (locationId) => {
          try {
            const result = await api.sde.getLocationName(locationId);
            return { locationId, name: result.name };
          } catch {
            return { locationId, name: `Location ${locationId}` };
          }
        });

        const locationNamesArray = await Promise.all(locationNamePromises);
        const locationNamesMap = locationNamesArray.reduce((acc, { locationId, name }) => {
          acc[locationId] = name;
          return acc;
        }, {} as Record<number, string>);
        setLocationNames(locationNamesMap);

      } catch (err) {
        console.error('Error fetching assets:', err);
        setError(err instanceof Error ? err.message : 'Failed to fetch assets');
      } finally {
        setLoading(false);
      }
    };

    fetchAssets();
  }, [navigate]);

  const groupByLocation = (assets: Asset[]): GroupedAssets => {
    return assets.reduce((groups, asset) => {
      const locationId = asset.locationId;
      if (!groups[locationId]) {
        groups[locationId] = [];
      }
      groups[locationId].push(asset);
      return groups;
    }, {} as GroupedAssets);
  };

  const getLocationName = (locationId: number): string => {
    return locationNames[locationId] || `Location ${locationId}`;
  };

  const getTypeName = (typeId: number): string => {
    return typeInfoCache[typeId]?.typeName || `Type ${typeId}`;
  };

  const getTypeGroup = (typeId: number): string => {
    return typeInfoCache[typeId]?.groupName || '';
  };

  const filteredAssets = assets.filter(asset => {
    const typeName = getTypeName(asset.typeId);
    const typeGroup = getTypeGroup(asset.typeId);
    const matchesSearch = searchTerm === '' || 
      typeName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      typeGroup.toLowerCase().includes(searchTerm.toLowerCase()) ||
      asset.typeId.toString().includes(searchTerm);
    
    const matchesLocation = selectedLocation === null || asset.locationId === selectedLocation;
    
    return matchesSearch && matchesLocation;
  });

  const groupedAssets = groupByLocation(filteredAssets);
  const locations = Object.keys(groupedAssets).map(Number).sort((a, b) => a - b);

  if (loading) {
    return (
      <div className="assets-container">
        <div className="loading">Loading assets...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="assets-container">
        <div className="error-box">
          <h2>Error</h2>
          <p>{error}</p>
          <button onClick={() => navigate('/characters')}>Back to Characters</button>
        </div>
      </div>
    );
  }

  return (
    <div className="assets-container">
      <div className="assets-header">
        <h1>Character Assets</h1>
        <div className="assets-stats">
          <span className="stat-badge">Total Items: {assets.length}</span>
          <span className="stat-badge">Locations: {Object.keys(groupedAssets).length}</span>
        </div>
      </div>

      <div className="assets-filters">
        <div className="search-box">
          <input
            type="text"
            placeholder="Search by type name or ID..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="search-input"
          />
        </div>

        <div className="filter-box">
          <label htmlFor="location-filter">Filter by Location:</label>
          <select
            id="location-filter"
            value={selectedLocation ?? ''}
            onChange={(e) => setSelectedLocation(e.target.value ? Number(e.target.value) : null)}
            className="filter-select"
          >
            <option value="">All Locations</option>
            {locations.map(locationId => (
              <option key={locationId} value={locationId}>
                {getLocationName(locationId)}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="assets-list">
        {locations.length === 0 ? (
          <div className="no-assets">
            <p>No assets found</p>
          </div>
        ) : (
          locations.map(locationId => (
            <div key={locationId} className="location-group">
              <div className="location-header">
                <h2>{getLocationName(locationId)}</h2>
                <span className="item-count">{groupedAssets[locationId].length} items</span>
              </div>
              <div className="assets-table-container">
                <table className="assets-table">
                  <thead>
                    <tr>
                      <th>Item Name</th>
                      <th>Group</th>
                      <th>Quantity</th>
                      <th>Location Flag</th>
                      <th>Blueprint</th>
                    </tr>
                  </thead>
                  <tbody>
                    {groupedAssets[locationId].map(asset => (
                      <tr key={asset.itemId}>
                        <td>
                          <div className="item-name-cell">
                            <img 
                              src={`https://images.evetech.net/types/${asset.typeId}/icon?size=32`}
                              alt={getTypeName(asset.typeId)}
                              className="item-icon"
                              onError={(e) => { e.currentTarget.style.display = 'none'; }}
                            />
                            <span>{getTypeName(asset.typeId)}</span>
                          </div>
                        </td>
                        <td>{getTypeGroup(asset.typeId)}</td>
                        <td className="quantity">{asset.quantity.toLocaleString()}</td>
                        <td>{asset.locationFlag}</td>
                        <td>
                          {asset.isBlueprintCopy !== null ? (
                            asset.isBlueprintCopy ? (
                              <span className="bp-badge bpc">BPC</span>
                            ) : (
                              <span className="bp-badge bpo">BPO</span>
                            )
                          ) : (
                            <span className="bp-badge na">N/A</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default Assets;
